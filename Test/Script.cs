using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;
using Sandbox.Game.Components;
using VRage.Game.Localization;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class Program : MyGridProgram
{
    static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");

    Timer timer;
    ShowMes Txt = new ShowMes();
    Camers camers = new Camers();
    static PID Rul = new PID();
    static IMyShipController RemCon = null;
    static MyDetectedEntityInfo Target;
    long TimeSkan = 0;


    Program()
    {
        RemCon = new Selection(null).FindBlock<IMyShipController>(GridTerminalSystem);
        if (RemCon == null) throw new Exception("Не найден блок управления");

        new Selection(null).FindBlocks<IMyCameraBlock>(GridTerminalSystem, camers);
        camers.ForEach(x => x.EnableRaycast = true);
        
        timer = new Timer(this);
        GridTerminalSystem.GetBlocksOfType<IMyGyro>(Rul);
        Rul.ForEach(x => x.GyroOverride = true);

        Txt.Txt = new Selection(null).FindBlock<IMyTextPanel>(GridTerminalSystem);

        Echo("Инициализировано");
    }


    void Main(string arg, UpdateType UT)
    {
        try
        {Runtime.UpdateFrequency = UpdateFrequency.None == Runtime.UpdateFrequency?UpdateFrequency.Update100:UpdateFrequency.None;
            if (UT >= UpdateType.Update1)
            {
                if (Target.IsEmpty()) { timer.Stop(); return; }
                Echo("*" + UT);
                var tc = timer.Run();

                if (tc == 0) return;
                Txt.SetPoint();
                TimeSkan += tc;
                var TarPos = Target.Position + (Target.Velocity * ((float)TimeSkan / 960));
                Txt.AddLine($"Speed: {Target.Velocity.Length()} * {(float)TimeSkan / 960}c.");
                var cam = camers.GetCamera(TarPos);
                if (cam != null)
                {
                    var Tar_ = cam.Raycast(TarPos);
                    if (!Tar_.IsEmpty())
                        if (Tar_.Type != MyDetectedEntityType.LargeGrid && Tar_.Type != MyDetectedEntityType.SmallGrid)
                            Target = new MyDetectedEntityInfo();
                        else
                        {
                            if (Target.TimeStamp == 1 && Target.Type == MyDetectedEntityType.Unknown) // Отправим сообщение
                            {
                                var ant = new Selection(null).FindBlock<IMyRadioAntenna>(GridTerminalSystem);
                                if (ant != null && ant.TransmitMessage("OK" + Target.EntityId))
                                    Txt.AddLine("Отправка сообщения OK : " + ant.CustomName).ToConsole = true;
                            }
                            TimeSkan = 0;
                            Target = Tar_;
                        }
                }

                if (Target.IsEmpty())
                    { Txt.AddLine("Цель потеряна " + cam.CustomName + "\n" + MyGPS.GPS("Потеря цели", TarPos)).Added = true; timer.Stop(); return; }
                else if (cam == null)
                    Txt.AddLine("Камеры не готовы..." + (TimeSkan/960) + MyGPS.GPS("Ц" + Target.TimeStamp, TarPos));
                else Txt.AddLine(cam.CustomName + "\n" + MyGPS.GPS(Target.Name, Target.Position - cam.GetPosition()));

                var Vdr = Rul.Rules();
                Txt.AddLine(string.Format("\nYaw: {0:0.00} Pitch: {1:0.00} Roll: {2:0.00}", MathHelper.ToDegrees(Vdr.X), MathHelper.ToDegrees(Vdr.Y), MathHelper.ToDegrees(Vdr.Z)));
                Txt.Show(this);
            }
            else if (UT == UpdateType.Antenna)
                AntenaMessage(arg);
            else if (!string.IsNullOrWhiteSpace(arg))
                SetAtributes(arg);
        }
        catch (Exception e)
        {
            Echo(e.ToString());
            Me.CustomData += e.ToString();
        }
    }

    void SetAtributes(string arg, bool ShowInfo = false)
    {
        var param = EndCut(ref arg, ":");
        switch (arg.ToLower())
        {
            case "?":
                SetAtributes(param, true);
                break;
            case "target":
                if (ShowInfo) Txt.AddLine(Target.Type == MyDetectedEntityType.None? "Not found": MyGPS.GPS(Target.Name, Target.Position));
                else
                {
                    Vector3D tmp;
                    MyGPS.TryParseVers(param, out tmp);
                    SetTarget(tmp);
                }
                break;
            default: Echo("Команда не опознана: " + arg); break;
        }
    }

    void AntenaMessage(string arg)
    {
        var ars = arg.Split('#');
        if (ars.Length < 4) return;
        bool isUpdate = false;

        if (ars[0] == "KillFree")
            { if (!Target.IsEmpty()) return; }
        else if (ars[0] == "NewPos")
            if (!Target.IsEmpty()) return;
            else isUpdate = true;
        else if (ars[0] != "Kill")
            { Txt.AddLine("Проигнорирована команда: " + arg).Show(this); return; }

        var id = long.Parse(ars[1]);
        if (isUpdate && id != Target.EntityId) return;
        Vector3D tmp, vel;
        Vector3D.TryParse(ars[2], out tmp);
        Vector3D.TryParse(ars[3], out vel);
        Target = new MyDetectedEntityInfo(id, "SetTerget", MyDetectedEntityType.Unknown, tmp, new MatrixD(), vel, MyRelationsBetweenPlayerAndBlock.Enemies, new BoundingBoxD(), 1);
        TimeSkan = 0;
        if (Txt.Txt!=null) Txt.Txt.CustomData += $"\n{arg}\n{MyGPS.GPS("o", tmp)}";
        timer.SetInterval(960, true);
    }

     void SetTarget(Vector3D Targ)
    {
        if (Targ == Vector3D.Zero) return;
        var cam = camers.GetCamera(Targ);
        if (cam == null) { Txt.AddLine("\nНет камеры для инициализации").ToConsole = true; return; }
        Target = cam.Raycast(Targ);
        if (Target.Type != MyDetectedEntityType.LargeGrid && Target.Type != MyDetectedEntityType.SmallGrid)
            Target = new MyDetectedEntityInfo();
        if (Target.IsEmpty()) { timer.Stop(); return; }
        else timer.SetInterval(960, true);
        TimeSkan = 0;
    }

    static string EndCut(ref string val, string tc, int cou = 1)
    {
        int pos = -1;
        while (--cou >= 0 && ++pos >= 0) if ((pos = val.IndexOf(tc, pos)) < 0) break;
        if (pos < 0) return string.Empty;
        var Result = val.Substring(pos + 1);
        val = val.Remove(pos);
        return Result;
    }

    //----------------   Classes  
    public class PID : List<IMyGyro>
    {
        public Vector3 Rules()
        {
            Vector3 Vdr = Vector3D.Zero;

            Vector3D vNap = Target.Position - RemCon.GetPosition();

            Vdr.X = GetAngel(RemCon.WorldMatrix.Down, RemCon.WorldMatrix.Forward, vNap);
            Vdr.Y = GetAngel(RemCon.WorldMatrix.Right, RemCon.WorldMatrix.Forward, vNap);

            var abs = Vector3D.Abs(Vdr);
            if (abs.X < 0.004f) Vdr.X = 0;
            if (abs.Y < 0.004f) Vdr.Y = 0;

            if (Vdr.X != 0 || Vdr.Y != 0 || Vdr.Z != 0) Drive(Vdr.X, Vdr.Y, Vdr.Z, RemCon.WorldMatrix);
            return Vdr;
        }
        public void Drive(double yaw_speed, double pitch_speed, double roll_speed, MatrixD shipMatrix)
        {
            if (Count == 0) return;
            var relativeRotationVec = Vector3D.TransformNormal(new Vector3D(-pitch_speed, yaw_speed, roll_speed), shipMatrix);
            foreach (var thisGyro in this)
            {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                thisGyro.GyroOverride = true;
                // mes = string.Format("{0:0.000} {1:0.000} {2:0.000} {3}", thisGyro.Yaw, thisGyro.Pitch, thisGyro.Roll, transformedRotationVec.ToString("0.000"));
                thisGyro.Pitch = (float)transformedRotationVec.X - (transformedRotationVec.X > 0.5 ? thisGyro.Pitch / 3 : 0);
                thisGyro.Yaw = (float)transformedRotationVec.Y - (transformedRotationVec.Y > 0.5 ? thisGyro.Yaw / 3 : 0);
                thisGyro.Roll = (float)transformedRotationVec.Z - (transformedRotationVec.Z > 0.5 ? thisGyro.Roll / 3 : 0);
            }
        }

        /// <summary> 
        ///  Расчитывает угол поворота 
        /// </summary> 
        /// <param name="Pl">Плоскость</param>
        /// <param name="VDirect">Вектор поворота</param> 
        /// <param name="Targ">Вектор цели</param> 
        public static float GetAngel(Vector3D Pl, Vector3D VDirect, Vector3D Targ)
        {
            var tm = Vector3D.Reject(Targ, Pl);
            var u = Math.Acos(VDirect.Dot(tm) / (VDirect.Length() * tm.Length()));
            //return (float)(MyMath.AngleBetween(tm, Pl.Cross(VDirect)) > MathHelper.PiOver2 ? -u : u);
            return (float)(tm.Dot(Pl.Cross(VDirect)) > 0 ? u : -u);
        }
    }

    public class ShowMes
    {
        List<String> buf = new List<string>();
        public IMyTextPanel Txt = null;
        int curPoint = 0;
        public bool ToConsole, Added = false;

        public void SetPoint() { curPoint = buf.Count; }
        public void Clear(bool all = false)
        {
            if (all) buf.Clear(); else buf.RemoveRange(0, curPoint);
            ToConsole = false;
            Added = false;
        }
        public void Show(MyGridProgram GS)
        {
            if (!Added) Clear();
            var text = string.Join("", buf);
            if (ToConsole || (Txt == null)) GS.Echo(text);
            if (Txt != null) Txt.WritePublicText(text);
        }
        public void Show(MyGridProgram GS, bool ToConsole, bool Added)
        {
            this.Added = Added;
            this.ToConsole = ToConsole;
            Show(GS);
        }

        public ShowMes Add(string text) { buf.Add(text); return this; }
        public ShowMes AddLine(string text) => Add(text + "\n");
        public ShowMes AddFromLine(string text) => Add("\n" + text);

    }

    public class Camers : List<IMyCameraBlock>
    {
        public IMyCameraBlock GetCamera(Vector3D Pos)
        {
            if (Count == 0) return null;
            return Find(x => x.CanScan(Pos));
        }
    }

    public static class MyGPS
    {
        public static string GPS(string Name, Vector3D Val)
        { return string.Format("GPS:{0}:{1:0.##}:{2:0.##}:{3:0.##}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
        public static string GPS(string Name, Vector3D Direct, Vector3D Pos, double dist = 0, string format = "")
        {
            Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
            return string.Format("GPS:{0}:{1}:{2}:{3}:",
                Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format));
        }
        public static string Vec_GPS(string Name, Vector3D Direct, Vector3D Pos, double dist, string format = "")
        {
            Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
            return string.Format("{0}:{4}\nGPS:{0}:{1}:{2}:{3}:",
              Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format), Direct.ToString(format));
        }
        public static bool TryParse(string vector, out Vector3D res)
        {
            var p = vector.Split(':');
            if (p.GetLength(0) == 6)
            {
                float x, y, z;
                if (float.TryParse(p[2], out x) && float.TryParse(p[3], out y) && float.TryParse(p[4], out z))
                    res = new Vector3D(x, y, z);
                else
                    res = Vector3D.Zero;
            }
            else res = Vector3D.Zero;
            return res != Vector3D.Zero;
        }
        public static bool TryParseVers(string vector, out Vector3D res)
        {
            if (!vector.StartsWith("{")) return TryParse(vector, out res);
            return Vector3D.TryParse(vector, out res);
        }
    }
    public class Selection
    {
        byte key = 0; string Val;
        public bool inv; public IMyCubeGrid GR = null;
        public string Value { get { return Val; } set { SetSel(value); } }

        public Selection(string val, IMyCubeGrid grid = null) { Value = val; GR = grid; }
        public Selection Change(string val) { Value = val; return this; }
        void SetSel(string val)
        {
            Val = ""; key = 0;
            if (string.IsNullOrEmpty(val)) return;
            inv = val.StartsWith("!"); if (inv) val = val.Remove(0, 1);
            if (string.IsNullOrEmpty(val)) return;
            int Pos = val.IndexOf('*', 0, 1) + val.LastIndexOf('*', val.Length - 1, 1) + 2;
            if (Pos == 0) Pos = 0;
            else if (Pos == 1) Pos = 1;
            else if (Pos == val.Length) Pos = 2;
            else Pos = Pos < 4 ? 1 : 3;
            if (Pos != 0)
            {
                if (Pos != 2) val = val.Remove(0, 1);
                if (Pos != 1) val = val.Remove(val.Length - 1, 1);
            }
            Val = val; key = (byte)Pos;
        }
        public override string ToString() { return inv ? "!" : ""; }
        public bool Complies(IMyTerminalBlock val)
        {
            if (GR != null && val.CubeGrid != GR) return false;
            if (!Complies(val.CustomName)) return false;
            return true;
        }
        public bool Complies(string str)
        {
            if (string.IsNullOrEmpty(Val)) return !inv;
            switch (key)
            {
                case 0: return str == Val != inv;
                case 1: return str.EndsWith(Val) != inv;
                case 2: return str.StartsWith(Val) != inv;
                case 3: return str.Contains(Val) != inv;
            }
            return false;
        }

        public Type FindBlock<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class
        {
            List<Type> res = new List<Type>(); bool fs = false;
            TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))));
            return res.Count == 0 ? null : res[0];
        }
        public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null)
        {
            TB.SearchBlocksOfName(inv ? "" : Val, res, x => Complies(x) && (Fp == null || Fp(x)));
        }
        public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
        { TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))); }
    }
    class Timer
    {
        readonly MyGridProgram GP;
        int Int;
        public bool zeroing;
        public int TC { get; protected set; }
        public Timer(MyGridProgram Owner, int Inter = 0, int tc = 0, bool zer = false)
        {
            GP = Owner;
            if (Inter == 0) zeroing = zer;
            else
            {
                var P = new Point(Inter, 0);
                CallFrequency(ref P);
                SetInterval(P, zer);
            }
            TC = tc;
        }
        public int Interval
        {
            get { return Int; }
            set { SetInterval(RoundInt(value), zeroing); }
        }
        public void Stop() { GP.Runtime.UpdateFrequency = UpdateFrequency.None; Int = 0; }
        public static Point RoundInt(int value)
        {
            if (value == 0) return new Point(0, 0);
            Point v = new Point(value, 0);
            var del = CallFrequency(ref v);
            v.X = value % del;
            if (v.X > del / 2) value += del;
            v.X = value - v.X;
            return v;
        }
        public static int CallFrequency(ref Point res)
        {
            int del;
            if (res.X < 960) { del = 16; res.Y = 1; }
            else if (res.X < 4000) { del = 160; res.Y = 2; }
            else { del = 1600; res.Y = 4; }
            return del;
        }
        public void SetInterval(int value, UpdateFrequency updateFreq, bool zeroing)
        { this.zeroing = zeroing; Int = value; GP.Runtime.UpdateFrequency = updateFreq; TC = 0; }
        public void SetInterval(Point val, bool zeroing)
        { this.zeroing = zeroing; Int = val.X; GP.Runtime.UpdateFrequency = (UpdateFrequency)val.Y; TC = 0; }
        public void SetInterval(int value, bool zeroing) { this.zeroing = zeroing; Interval = value; }
        public int Run()
        {
            if (Int == 0) return 1;
            TC += (int)GP.Runtime.TimeSinceLastRun.TotalMilliseconds;
            if (TC < Int) return 0;
            int res = TC;
            TC = zeroing ? 0 : TC % Int;
            return res;
        }
        public override string ToString() { return Int == 0 ? "отключено" : (Int + "мс:" + GP.Runtime.UpdateFrequency); }
        public string ToString(int okr) => Int == 0 ? "отключено" : GetInterval().ToString("##0.##");
        public double GetInterval(int okr = 1000)
        {
            if (Int == 0) return 0;
            if (!zeroing) return (double)Int / okr;
            int i;
            switch (GP.Runtime.UpdateFrequency)
            {
                case UpdateFrequency.Update1: i = 16; break;
                case UpdateFrequency.Update10: i = 160; break;
                case UpdateFrequency.Update100: i = 1600; break;
                default: return -1;
            }
            var b = Int % i;
            b = b == 0 ? Int : (Int / i + 1) * i;
            return ((double)b / okr);
        }
        public string ToSave() => $"{TC}@{zeroing}@{Int}";
        public static Timer Parse(string sv, MyGridProgram gp)
        {
            var s = sv.Split('@');
            return new Timer(gp, int.Parse(s[2]), int.Parse(s[0]), bool.Parse(s[1]));
        }
    }

}



using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;
using Sandbox.Game.Components;
using VRage.Game.Localization;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace P2
{
    class Program : MyGridProgram
    {
        /*----------------------------------------------------------------------  
        AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com  
        When using and disseminating information about the authorship is obligatory  
        При использовании и распространении информация об авторстве обязательна  
        ----------------------------------------------------------------------*/

        static MyDetectedEntityInfo Target = new MyDetectedEntityInfo();
        ShowMes Txt = new ShowMes();
        static Camers camers = new Camers();
        static IMyRadioAntenna ant = null;
        Timer timer;
        long TimeSkan = 0;

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;

            var ars = Me.CustomData.Split(';');
            if (ars.Length > 0)
            {
                Txt.Txt = new Selection(ars[0]).FindBlock<IMyTextPanel>(GridTerminalSystem);
                if (Txt.Txt == null) Echo($"Не найдена панель \"{ars[0]}\"");
            }

            new Selection(null).FindBlocks<IMyCameraBlock>(GridTerminalSystem, camers);
            camers.ForEach(x => x.EnableRaycast = true);

            ant = new Selection(null).FindBlock<IMyRadioAntenna>(GridTerminalSystem);
            if (ant == null) Echo("Не найдена антена для передачи цели");
            else ant.AttachedProgrammableBlock = Me.EntityId;

            timer = new Timer(this);
            Echo("Инициализировано\nКамер найдено: " + camers.Count);
        }


        void Main(string arg, UpdateType UT)
        {
            // if (UT < UpdateType.Update1 && !string.IsNullOrWhiteSpace(arg)) { SetAtributes(arg); return; }
            try
            {
                Txt.SetPoint();
                if (UT == UpdateType.Antenna)
                {
                    if (Runtime.UpdateFrequency == UpdateFrequency.None) return;
                    TimeSkan += (long)Runtime.TimeSinceLastRun.TotalMilliseconds;
                    if (!arg.StartsWith("OK")) return;
                    long tmp;
                    if (!long.TryParse(arg.Substring(2), out tmp)) return;
                    if (tmp != Target.EntityId) return;
                    timer.Stop();
                    Txt.AddLine("Цель передана ракетоносителю").Added = true;
                }
                else if (UT < UpdateType.Update1)
                { // ручной запуск
                    if (arg == "*")
                    {
                        TimeSkan += (long)Runtime.TimeSinceLastRun.TotalMilliseconds;
                        Txt.Add(arg).Show(this);
                        return;
                    }
                    if (Runtime.UpdateFrequency != UpdateFrequency.None) timer.Stop();

                    var cam = new Selection(null).FindBlock<IMyCameraBlock>(GridTerminalSystem, x => x.IsActive);
                    if (cam == null) { Txt.AddLine("Активная камера не найдена").Show(this, true, false); return; }

                    int Dist = 10000;
                    if (!string.IsNullOrWhiteSpace(arg)) if (!int.TryParse(arg, out Dist)) Dist = 10000;
                    Txt.AddLine(MyGPS.GPS("Vector", cam.WorldMatrix.Forward, cam.GetPosition(), Dist));

                    Target = cam.Raycast(Dist);
                    if (Target.Type != MyDetectedEntityType.SmallGrid && Target.Type != MyDetectedEntityType.LargeGrid)
                    { Txt.AddLine($"Цель на растоянии {Dist} м. не найдена").Show(this, true, false); return; }

                    Txt.AddLine(string.Format("{0} ({1}) {2}\n{3}", Target.Name, Target.Type, Target.Position, MyGPS.GPS("Цель", Target.Position)));
                    new Selection(null).ActionBlock<IMySoundBlock>(GridTerminalSystem, x => x.Play());

                    if (ant == null)
                        Txt.AddLine("Не установлена антена").ToConsole = true;
                    else if (!ant.TransmitMessage($"KillFree#{Target.EntityId}#{Target.Position}#{Target.Velocity}"))
                        Txt.AddLine("Отправка сообщения не удалась").ToConsole = true;
                    else
                    {
                        TimeSkan = 0;
                        timer.SetInterval(960, true); // включаем отслеживание цели
                    }
                }
                else
                // Отслежавание цели
                {
                    var tc = timer.Run();
                    if (tc == 0) return;
                    TimeSkan += tc;
                    var TarPos = Target.Position + (Target.Velocity * ((float)TimeSkan / 960));
                    Txt.AddLine($"Скорость {Target.Velocity.Length()} за {(float)TimeSkan / 960}c. = {Target.Velocity.Length() * ((float)TimeSkan / 960)}").Added = true;
                    var cam = camers.GetCamera(TarPos);
                    if (cam != null)
                    {
                        Target = cam.Raycast(TarPos);
                        if (Target.Type != MyDetectedEntityType.LargeGrid && Target.Type != MyDetectedEntityType.SmallGrid)
                            Target = new MyDetectedEntityInfo();
                        else TimeSkan = 0;
                    }

                    if (Target.IsEmpty()) { timer.Stop(); Txt.AddLine($"Цель потеряна {cam.CustomName} /{TarPos - Target.Position}\n{MyGPS.GPS("Потеря цели", TarPos)}"); return; }
                    else if (cam == null) Txt.AddLine("Камеры не готовы..." + TimeSkan + MyGPS.GPS("Ц" + Target.TimeStamp, TarPos));
                    else
                    {
                        Txt.AddLine(cam.CustomName + "\n" + MyGPS.GPS(Target.Name, Target.Position - cam.GetPosition())).Added = false;
                        if (ant == null) Txt.AddLine("Не указана антена").ToConsole = true;
                        else if (!ant.TransmitMessage($"NewPos#{Target.EntityId}#{Target.Position}#{Target.Velocity}"))
                            Txt.AddLine("Не удалась отправка сообщения антеной -" + ant.CustomName).ToConsole = true;
                        else
                            Txt.AddLine("Отправлено сообщение антеной -" + ant.CustomName).ToConsole = true;
                    }
                }
                Txt.Show(this);
            }
            catch (Exception e)
            {
                Echo(e.ToString());
                Me.CustomData += e.ToString();
            }
        }

        static string EndCut(ref string val, string tc, int cou = 1)
        {
            int pos = -1;
            while (--cou >= 0 && ++pos >= 0) if ((pos = val.IndexOf(tc, pos)) < 0) break;
            if (pos < 0) return string.Empty;
            var Result = val.Substring(pos + 1);
            val = val.Remove(pos);
            return Result;
        }

        //----------------   Classes  

        public class Camers : List<IMyCameraBlock>
        {
            public IMyCameraBlock GetCamera(Vector3D Pos)
            {
                if (Count == 0) return null;
                return Find(x => x.CanScan(Pos));
            }
            /*public int tec = 0;
            public IMyCameraBlock GetCamera(Vector3D Pos)
            {
                if (Count == 0) return null;
                if (!this[tec].CanScan(Pos)) return null;
                var tc = this[tec];
                if (!tc.IsWorking) { Remove(tc); return GetCamera(Pos); }
                else if (++tec == Count) tec = 0;
                return tc;
            }*/
        }

        public class ShowMes
        {
            List<String> buf = new List<string>();
            public IMyTextPanel Txt = null;
            int curPoint = 0;
            public bool ToConsole, Added = false;

            public void SetPoint() { curPoint = buf.Count; }
            public void Clear(bool all = false)
            {
                if (all) buf.Clear(); else buf.RemoveRange(0, curPoint);
                ToConsole = false;
                Added = false;
            }
            public void Show(MyGridProgram GS)
            {
                if (!Added) Clear();
                var text = string.Join("", buf);
                if (ToConsole || (Txt == null)) GS.Echo(text);
                if (Txt != null) Txt.WritePublicText(text);
            }
            public void Show(MyGridProgram GS, bool ToConsole, bool Added)
            {
                this.Added = Added;
                this.ToConsole = ToConsole;
                Show(GS);
            }

            public ShowMes Add(string text) { buf.Add(text); return this; }
            public ShowMes AddLine(string text) => Add(text + "\n");
            public ShowMes AddFromLine(string text) => Add("\n" + text);

        }

        public static class MyGPS
        {
            public static string GPS(string Name, Vector3D Val)
            { return string.Format("GPS:{0}:{1:0.##}:{2:0.##}:{3:0.##}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
            public static string GPS(string Name, Vector3D Direct, Vector3D Pos, double dist = 0, string format = "")
            {
                Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
                return string.Format("GPS:{0}:{1}:{2}:{3}:",
                    Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format));
            }
            public static string Vec_GPS(string Name, Vector3D Direct, Vector3D Pos, double dist, string format = "")
            {
                Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
                return string.Format("{0}:{4}\nGPS:{0}:{1}:{2}:{3}:",
                  Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format), Direct.ToString(format));
            }
            public static bool TryParse(string vector, out Vector3D res)
            {
                var p = vector.Split(':');
                if (p.GetLength(0) == 6)
                {
                    float x, y, z;
                    if (float.TryParse(p[2], out x) && float.TryParse(p[3], out y) && float.TryParse(p[4], out z))
                        res = new Vector3D(x, y, z);
                    else
                        res = Vector3D.Zero;
                }
                else res = Vector3D.Zero;
                return res != Vector3D.Zero;
            }
            public static bool TryParseVers(string vector, out Vector3D res)
            {
                if (!vector.StartsWith("{")) return TryParse(vector, out res);
                return Vector3D.TryParse(vector, out res);
            }
        }
        public class Selection
        {
            byte key = 0; string Val;
            public bool inv; public IMyCubeGrid GR = null;
            public string Value { get { return Val; } set { SetSel(value); } }

            public Selection(string val, IMyCubeGrid grid = null) { Value = val; GR = grid; }
            public Selection Change(string val) { Value = val; return this; }
            void SetSel(string val)
            {
                Val = ""; key = 0;
                if (string.IsNullOrEmpty(val)) return;
                inv = val.StartsWith("!"); if (inv) val = val.Remove(0, 1);
                if (string.IsNullOrEmpty(val)) return;
                int Pos = val.IndexOf('*', 0, 1) + val.LastIndexOf('*', val.Length - 1, 1) + 2;
                if (Pos == 0) Pos = 0;
                else if (Pos == 1) Pos = 1;
                else if (Pos == val.Length) Pos = 2;
                else Pos = Pos < 4 ? 1 : 3;
                if (Pos != 0)
                {
                    if (Pos != 2) val = val.Remove(0, 1);
                    if (Pos != 1) val = val.Remove(val.Length - 1, 1);
                }
                Val = val; key = (byte)Pos;
            }
            public override string ToString() { return inv ? "!" : ""; }
            public bool Complies(IMyTerminalBlock val)
            {
                if (GR != null && val.CubeGrid != GR) return false;
                if (!Complies(val.CustomName)) return false;
                return true;
            }
            public bool Complies(string str)
            {
                if (string.IsNullOrEmpty(Val)) return !inv;
                switch (key)
                {
                    case 0: return str == Val != inv;
                    case 1: return str.EndsWith(Val) != inv;
                    case 2: return str.StartsWith(Val) != inv;
                    case 3: return str.Contains(Val) != inv;
                }
                return false;
            }

            public Type FindBlock<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class
            {
                List<Type> res = new List<Type>(); bool fs = false;
                TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))));
                return res.Count == 0 ? null : res[0];
            }
            public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null)
            {
                TB.SearchBlocksOfName(inv ? "" : Val, res, x => Complies(x) && (Fp == null || Fp(x)));
            }
            public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
            { TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))); }

            public bool ActionBlock<Type>(IMyGridTerminalSystem TB, Action<Type> Act, Func<Type, bool> Fp = null) where Type : class
            {
                List<Type> res = new List<Type>(); bool fs = false;
                TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))));
                if (res.Count > 0) Act(res[0]);
                return res.Count > 0;
            }
            public int ActionBlocks<Type>(IMyGridTerminalSystem TB, Action<Type> Act, Func<Type, bool> Fp = null) where Type : class
            {
                List<Type> res = new List<Type>();
                TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x)));
                res.ForEach(x => Act(x));
                return res.Count;
            }
        }

        class Timer
        {
            readonly MyGridProgram GP;
            int Int;
            public bool zeroing;
            public int TC { get; protected set; }
            public Timer(MyGridProgram Owner, int Inter = 0, int tc = 0, bool zer = false)
            {
                GP = Owner;
                if (Inter == 0) zeroing = zer;
                else
                {
                    var P = new Point(Inter, 0);
                    CallFrequency(ref P);
                    SetInterval(P, zer);
                }
                TC = tc;
            }
            public int Interval
            {
                get { return Int; }
                set { SetInterval(RoundInt(value), zeroing); }
            }
            public void Stop() { GP.Runtime.UpdateFrequency = UpdateFrequency.None; Int = 0; }
            public static Point RoundInt(int value)
            {
                if (value == 0) return new Point(0, 0);
                Point v = new Point(value, 0);
                var del = CallFrequency(ref v);
                v.X = value % del;
                if (v.X > del / 2) value += del;
                v.X = value - v.X;
                return v;
            }
            public static int CallFrequency(ref Point res)
            {
                int del;
                if (res.X <= 960) { del = 16; res.Y = 1; }
                else if (res.X < 4000) { del = 160; res.Y = 2; }
                else { del = 1600; res.Y = 4; }
                return del;
            }
            public void SetInterval(int value, UpdateFrequency updateFreq, bool zeroing)
            { this.zeroing = zeroing; Int = value; GP.Runtime.UpdateFrequency = updateFreq; TC = 0; }
            public void SetInterval(Point val, bool zeroing)
            { this.zeroing = zeroing; Int = val.X; GP.Runtime.UpdateFrequency = (UpdateFrequency)val.Y; TC = 0; }
            public void SetInterval(int value, bool zeroing) { this.zeroing = zeroing; Interval = value; }
            public int Run()
            {
                if (Int == 0) return 1;
                TC += (int)GP.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (TC < Int) return 0;
                int res = TC;
                TC = zeroing ? 0 : TC % Int;
                return res;
            }
            public override string ToString() { return Int == 0 ? "отключено" : (Int + "мс:" + GP.Runtime.UpdateFrequency); }
            public string ToString(int okr) => Int == 0 ? "отключено" : GetInterval().ToString("##0.##");
            public double GetInterval(int okr = 1000)
            {
                if (Int == 0) return 0;
                if (!zeroing) return (double)Int / okr;
                int i;
                switch (GP.Runtime.UpdateFrequency)
                {
                    case UpdateFrequency.Update1: i = 16; break;
                    case UpdateFrequency.Update10: i = 160; break;
                    case UpdateFrequency.Update100: i = 1600; break;
                    default: return -1;
                }
                var b = Int % i;
                b = b == 0 ? Int : (Int / i + 1) * i;
                return ((double)b / okr);
            }
            public string ToSave() => $"{TC}@{zeroing}@{Int}";
            public static Timer Parse(string sv, MyGridProgram gp)
            {
                var s = sv.Split('@');
                return new Timer(gp, int.Parse(s[2]), int.Parse(s[0]), bool.Parse(s[1]));
            }
        }


    }
}


namespace P3
{
    class Program : MyGridProgram
    {

        Follower Follower1;
        static ShowMes txt = new ShowMes();
        Vector3D tecTarg;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main(string arg, UpdateType UT)
        {
            if (UT == UpdateType.Update100)
            {
                Follower1 = new Follower(this);
                txt.Txt = new Selection(null).FindBlock<IMyTextPanel>(GridTerminalSystem);
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }
            else if (UT == UpdateType.Update1)
            {
                //будем перемещаться к этой точке
                //GPS:Pennywise #1:-13010.66:50598.2:32509.12:
                //будем держать прицел на эту точку:
                //GPS:Pennywise #2:-12976.41:50487.77:32242.19:
                //Follower1.GoToPos(new Vector3D(-13010.66, 50598.2, 32509.12), new Vector3D(-12976.41, 50487.77, 32242.19));
                txt.SetPoint();
                if (!Vector3D.IsZero(tecTarg))
                {
                    Follower1.GoToPos(tecTarg);
                }
                txt.Show(this);
            }
            else if (UT == UpdateType.Terminal && !string.IsNullOrWhiteSpace(arg))
            {
                MyGPS.TryParseVers(arg, out tecTarg);
            }

        }

        public class Follower
        {
            static IMyShipController RemCon;
            Vector3D acceleration;
            static Program ParentProgram;
            MyThrusters myThr;
            MyGyros myGyros;
            MySensors mySensors;
            MyWeapons myWeapons;

            public Follower(Program parenProg)
            {
                ParentProgram = parenProg;
                InitMainBlocks();
                InitSubSystems();
                acceleration = myThr.GetMaxSpeed();
            }

            private void InitMainBlocks()
            {
                RemCon = new Selection(null).FindBlock<IMyShipController>(ParentProgram.GridTerminalSystem);
                if (RemCon == null) ParentProgram.Echo("Нет блока упаравления");
            }

            private void InitSubSystems()
            {
                myThr = new MyThrusters();
                myGyros = new MyGyros(this, 3);
                mySensors = new MySensors();
                myWeapons = new MyWeapons();
            }

            public void TestDrive(Vector3D Thr)
            {
                myThr.SetThrA(Thr);
            }

            public void TestHover()
            {
                Vector3D GravAccel = RemCon.GetNaturalGravity();
                //MatrixD MyMatrix = MatrixD.Invert(RemCon.WorldMatrix.GetOrientation());
                //myThr.SetThrA(Vector3D.Transform(-GravAccel, MyMatrix));

                MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
                myThr.SetThrA(VectorTransform(-GravAccel, MyMatrix));
            }


            public void Killing()
            {
                mySensors.UpdateSensors();
                if (mySensors.DetectedOwner.HasValue)
                {
                    GoToPos(mySensors.DetectedOwner.Value.Position - RemCon.GetNaturalGravity());
                }
                else
                {
                    //Здесь что-то надо делать, если потерян контакт с хозяином
                }
                if (mySensors.DetectedEnemy.HasValue)
                {
                    Fire(mySensors.DetectedEnemy.Value.Position);
                }
                else
                {
                    //Здесь не обнаружен враг
                }
            }

            public void Fire(Vector3D Pos)
            {
                MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
                if (myGyros.LookAtPoint(VectorTransform(mySensors.DetectedEnemy.Value.Position - RemCon.GetPosition(), MyMatrix)) < 0.1)
                {
                    myWeapons.Fire();
                }
            }

            public void GoToPos(Vector3D Pos)
            {
                //Расчитать расстояние до цели
                var TargetVector = Pos - RemCon.GetPosition();
                var dist = TargetVector.Length();
                //var dist = TargetVector.Length();
                //Расчитать желаемую скорость
                Vector3D DesiredVelocity = TargetVector * Math.Sqrt(2 * acceleration.AbsMin() / dist);
                Vector3D VelocityDelta = DesiredVelocity - RemCon.GetShipVelocities().LinearVelocity;
                //Расчитать желаемое ускорение
                Vector3D DesiredAcceleration = VelocityDelta;
                if (dist > 5) DesiredAcceleration *= acceleration.AbsMin() * 2;
                 var ttt = VectorTransform(DesiredAcceleration -  RemCon.GetNaturalGravity(), RemCon.WorldMatrix.GetOrientation());
                //Передаем желаемое ускорение с учетом гравитации движкам
                myThr.SetThrA(ttt);
            }

            public Vector3D VectorTransform(Vector3D Vec, MatrixD Orientation)
            {
                return new Vector3D(Vec.Dot(Orientation.Right), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Backward));
            }

            private class MyWeapons : List<IMySmallGatlingGun>
            {
                public MyWeapons()
                {
                    InitMainBlocks();
                }

                private void InitMainBlocks()
                {
                    ParentProgram.GridTerminalSystem.GetBlocksOfType<IMySmallGatlingGun>(this);
                }

                public void Fire()
                {
                    foreach (IMySmallGatlingGun gun in this)
                    {
                        gun.ApplyAction("ShootOnce");
                    }
                }

            }

            private class MySensors : List<IMySensorBlock>
            {
                public List<MyDetectedEntityInfo> DetectedEntities;
                public MyDetectedEntityInfo? DetectedOwner;
                public MyDetectedEntityInfo? DetectedEnemy;
                string OwnerName;

                public MySensors()
                {
                    InitMainBlocks();
                    OwnerName = ParentProgram.Me.CustomData;
                }

                private void InitMainBlocks()
                {
                    DetectedEntities = new List<MyDetectedEntityInfo>();
                    ParentProgram.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(this);
                }

                public void UpdateSensors()
                {
                    DetectedOwner = null;
                    DetectedEnemy = null;
                    foreach (IMySensorBlock sensor in this)
                    {
                        sensor.DetectedEntities(DetectedEntities);
                        foreach (MyDetectedEntityInfo detEnt in DetectedEntities)
                        {
                            ParentProgram.Echo(detEnt.Name);
                            if (!DetectedOwner.HasValue && detEnt.Name == OwnerName)
                            {
                                DetectedOwner = detEnt;
                            }
                            else if (!DetectedEnemy.HasValue && detEnt.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                            {
                                DetectedEnemy = detEnt;
                            }

                        }
                    }
                }

            }

            private class MyGyros : List<IMyGyro>
            {
                float gyroMult;
                Follower myBot;

                public MyGyros(Follower mbt, float mult)
                {
                    myBot = mbt;
                    gyroMult = mult;
                    InitMainBlocks();
                }

                private void InitMainBlocks()
                {
                    ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyGyro>(this);
                }

                public float LookAtPoint(Vector3D LookPoint)
                {
                    Vector3D SignalVector = Vector3D.Normalize(LookPoint);
                    foreach (IMyGyro gyro in this)
                    {
                        gyro.Pitch = -(float)SignalVector.Y * gyroMult;
                        gyro.Yaw = (float)SignalVector.X * gyroMult;
                    }
                    return (Math.Abs((float)SignalVector.Y) + Math.Abs((float)SignalVector.X));
                }

            }

            private class MyThrusters : List<IMyThrust>
            {
                public class GroupThrusts : List<IMyThrust>
                {
                    public double EffectivePow { get; protected set; }
                    new public void Add(IMyThrust val) { base.Add(val); EffectivePow += val.MaxEffectiveThrust; }
                    new public void Clear() { EffectivePow = 0; base.Clear(); }
                    public void Recalc() { EffectivePow = 0; ForEach(x => EffectivePow += x.MaxEffectiveThrust); }
                }

                //Follower myBot;
                GroupThrusts UpThrusters;
                GroupThrusts DownThrusters;
                GroupThrusts LeftThrusters;
                GroupThrusts RightThrusters;
                GroupThrusts ForwardThrusters;
                GroupThrusts BackwardThrusters;

                public struct ThrustsValue
                {
                    public double Max_pow, EffectivePow, CurrentPow;
                    public void Add(IMyThrust val) { Max_pow += val.MaxThrust; EffectivePow += val.MaxEffectiveThrust; CurrentPow += val.CurrentThrust; }
                    public void Clear() { Max_pow = 0; EffectivePow = 0; CurrentPow = 0; }
                    public double TecCoof { get { return EffectivePow / Max_pow; } }
                    public new string ToString() => $"M:{Max_pow} E:{EffectivePow}, C:{CurrentPow}";
                }

                public Vector3D GetMaxSpeed()
                {
                    var m= RemCon.CalculateShipMass().TotalMass;
                    return new Vector3D(
                        Math.Min(RightThrusters.EffectivePow, LeftThrusters.EffectivePow / m),
                        Math.Min(UpThrusters.EffectivePow, DownThrusters.EffectivePow / m),
                        Math.Min(ForwardThrusters.EffectivePow, BackwardThrusters.EffectivePow / m));
                }


                //переменные подсистемы двигателей
                public MyThrusters()
                {
                    InitMainBlocks();
                }

                private void InitMainBlocks()
                {
                    UpThrusters = new GroupThrusts();
                    DownThrusters = new GroupThrusts();
                    LeftThrusters = new GroupThrusts();
                    RightThrusters = new GroupThrusts();
                    ForwardThrusters = new GroupThrusts();
                    BackwardThrusters = new GroupThrusts();

                    ReloadTrusters();
                }

                public void ReloadTrusters()
                {
                    UpThrusters.Clear();
                    DownThrusters.Clear();
                    LeftThrusters.Clear();
                    RightThrusters.Clear();
                    ForwardThrusters.Clear();
                    BackwardThrusters.Clear();

                    Matrix ThrLocM = new Matrix();
                    Matrix MainLocM = new Matrix();
                    RemCon.Orientation.GetMatrix(out MainLocM);

                    ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyThrust>(this, x=>x.IsWorking);

                    for (int i = 0; i < Count; i++)
                    {
                        IMyThrust Thrust = this[i];
                        Thrust.Orientation.GetMatrix(out ThrLocM);
                        //Y
                        if (ThrLocM.Backward == MainLocM.Up)
                            UpThrusters.Add(Thrust);
                        else if (ThrLocM.Backward == MainLocM.Down)
                            DownThrusters.Add(Thrust);
                        //X
                        else if (ThrLocM.Backward == MainLocM.Left)
                            LeftThrusters.Add(Thrust);
                        else if (ThrLocM.Backward == MainLocM.Right)
                            RightThrusters.Add(Thrust);
                        //Z
                        else if (ThrLocM.Backward == MainLocM.Forward)
                            ForwardThrusters.Add(Thrust);
                        else if (ThrLocM.Backward == MainLocM.Backward)
                            BackwardThrusters.Add(Thrust);
                    }
                }

                private void SetGroupThrust(List<IMyThrust> ThrList, float Thr)
                {
                    for (int i = 0; i < ThrList.Count; i++)
                    {
                        //ThrList[i].SetValue("Override", Thr); //OldSchool
                        ThrList[i].ThrustOverridePercentage = Thr;
                    }
                }

                public void SetThrF(Vector3D ThrVec)
                {
                    SetGroupThrust(this, 0f);
                    //X
                    if (ThrVec.X > 0)
                    {
                        SetGroupThrust(RightThrusters, (float)(ThrVec.X / RightThrusters.EffectivePow));
                        ParentProgram.Echo("R: " + ThrVec.X / RightThrusters.EffectivePow);
                    }
                    else
                    {
                        SetGroupThrust(LeftThrusters, -(float)(ThrVec.X / LeftThrusters.EffectivePow));
                        ParentProgram.Echo("L: " + (-ThrVec.X / LeftThrusters.EffectivePow));
                    }
                    //Y
                    if (ThrVec.Y > 0)
                    {
                        SetGroupThrust(UpThrusters, (float)(ThrVec.Y / UpThrusters.EffectivePow));
                        ParentProgram.Echo("U: " + ThrVec.Y / UpThrusters.EffectivePow);
                    }
                    else
                    {
                        SetGroupThrust(DownThrusters, -(float)(ThrVec.Y / DownThrusters.EffectivePow));
                        ParentProgram.Echo("D: " + (-ThrVec.Y / DownThrusters.EffectivePow));
                    }
                    //Z
                    if (ThrVec.Z > 0)
                    {
                        SetGroupThrust(BackwardThrusters, (float)(ThrVec.Z / BackwardThrusters.EffectivePow));
                        ParentProgram.Echo("B: " + ThrVec.Z / BackwardThrusters.EffectivePow);
                    }
                    else
                    {
                        SetGroupThrust(ForwardThrusters, -(float)(ThrVec.Z / ForwardThrusters.EffectivePow));
                        ParentProgram.Echo($"F: {-ThrVec.Z / ForwardThrusters.EffectivePow:0.0000} - {ForwardThrusters.EffectivePow:0.0000}");
                    }
                }
                public void SetThrA(Vector3D ThrVec)
                {
                    double PhysMass = RemCon.CalculateShipMass().PhysicalMass;
                    SetThrF(ThrVec * PhysMass);
                }


            }

        }
        public class ShowMes
        {
            List<String> buf = new List<string>();
            public IMyTextPanel Txt = null;
            int curPoint = 0;
            public bool ToConsole, Added = false;

            public void SetPoint() { curPoint = buf.Count; }
            public void Clear(bool all = false)
            {
                if (all) buf.Clear(); else buf.RemoveRange(0, curPoint);
                ToConsole = false;
                Added = false;
            }
            public void Show(MyGridProgram GS)
            {
                if (!Added) Clear();
                var text = string.Join("", buf);
                if (ToConsole || (Txt == null)) GS.Echo(text);
                if (Txt != null) Txt.WritePublicText(text);
            }
            public void Show(MyGridProgram GS, bool ToConsole, bool Added)
            {
                this.Added = Added;
                this.ToConsole = ToConsole;
                Show(GS);
            }

            public ShowMes Add(string text) { buf.Add(text); return this; }
            public ShowMes AddLine(string text) => Add(text + "\n");
            public ShowMes AddFromLine(string text) => Add("\n" + text);

        }

        public class Selection
        {
            byte key = 0; string Val;
            public bool inv; public IMyCubeGrid GR = null;
            public string Value { get { return Val; } set { SetSel(value); } }

            public Selection(string val, IMyCubeGrid grid = null) { Value = val; GR = grid; }
            public Selection Change(string val) { Value = val; return this; }
            void SetSel(string val)
            {
                Val = ""; key = 0;
                if (string.IsNullOrEmpty(val)) return;
                inv = val.StartsWith("!"); if (inv) val = val.Remove(0, 1);
                if (string.IsNullOrEmpty(val)) return;
                int Pos = val.IndexOf('*', 0, 1) + val.LastIndexOf('*', val.Length - 1, 1) + 2;
                if (Pos == 0) Pos = 0;
                else if (Pos == 1) Pos = 1;
                else if (Pos == val.Length) Pos = 2;
                else Pos = Pos < 4 ? 1 : 3;
                if (Pos != 0)
                {
                    if (Pos != 2) val = val.Remove(0, 1);
                    if (Pos != 1) val = val.Remove(val.Length - 1, 1);
                }
                Val = val; key = (byte)Pos;
            }
            public override string ToString() { return inv ? "!" : ""; }
            public bool Complies(IMyTerminalBlock val)
            {
                if (GR != null && val.CubeGrid != GR) return false;
                if (!Complies(val.CustomName)) return false;
                return true;
            }
            public bool Complies(string str)
            {
                if (string.IsNullOrEmpty(Val)) return !inv;
                switch (key)
                {
                    case 0: return str == Val != inv;
                    case 1: return str.EndsWith(Val) != inv;
                    case 2: return str.StartsWith(Val) != inv;
                    case 3: return str.Contains(Val) != inv;
                }
                return false;
            }

            public Type FindBlock<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class
            {
                List<Type> res = new List<Type>(); bool fs = false;
                TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))));
                return res.Count == 0 ? null : res[0];
            }
            public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null)
            {
                TB.SearchBlocksOfName(inv ? "" : Val, res, x => Complies(x) && (Fp == null || Fp(x)));
            }
            public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
            { TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))); }
        }

        public static class MyGPS
        {
            public static string GPS(string Name, Vector3D Val)
            { return string.Format("GPS:{0}:{1:0.##}:{2:0.##}:{3:0.##}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
            public static string GPS(string Name, Vector3D Direct, Vector3D Pos, double dist = 0, string format = "")
            {
                Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
                return string.Format("GPS:{0}:{1}:{2}:{3}:",
                    Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format));
            }
            public static string Vec_GPS(string Name, Vector3D Direct, Vector3D Pos, double dist, string format = "")
            {
                Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
                return string.Format("{0}:{4}\nGPS:{0}:{1}:{2}:{3}:",
                  Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format), Direct.ToString(format));
            }
            public static bool TryParse(string vector, out Vector3D res)
            {
                var p = vector.Split(':');
                if (p.GetLength(0) == 6)
                {
                    float x, y, z;
                    if (float.TryParse(p[2], out x) && float.TryParse(p[3], out y) && float.TryParse(p[4], out z))
                        res = new Vector3D(x, y, z);
                    else
                        res = Vector3D.Zero;
                }
                else res = Vector3D.Zero;
                return res != Vector3D.Zero;
            }
            public static bool TryParseVers(string vector, out Vector3D res)
            {
                if (!vector.StartsWith("{")) return TryParse(vector, out res);
                return Vector3D.TryParse(vector, out res);
            }
        }



    }
}


namespace P4
{
    class Program : MyGridProgram
    {

        /*--------------------------------------------------------------------------      
  AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: MyAndrey_ka@mail.ru       
  When using and disseminating information about the authorship is obligatory      
  При использовании и распространении информация об авторстве обязательна      
  ----------------------------------------------------------------------*/

        static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");
        Program()
        {
            try
            {
                if (!string.IsNullOrEmpty(Me.CustomData)) Init(Me.CustomData);
                GridTerminalSystem.GetBlocksOfType<IMyCameraBlock>(Camers,
                    x => x.Enabled && x.Orientation.Forward == RemCon.Orientation.Forward && (x.EnableRaycast = true));

                if (!Storage.StartsWith("AutoUp") || Me.TerminalRunArgument == "null")
                { T = new Timer(this); return; }

                var tm = Storage.Split('\n');
                CP = int.Parse(tm[1]);
                T = Timer.Parse(tm[2], this);
                Rul.Load(tm[3]);
            }
            catch (Exception e) { Echo(e.ToString()); Me.CustomData += "\n" + e.ToString(); }
        }

        void Save() { Storage = string.Format("AutoUp\n{0}\n{1}\n{2}", CP, T.ToSave(), Rul.ToSave()); }

        const int maxSpeed = 100;

        readonly Timer T;
        static IMyShipController RemCon = null;
        static IMyTextPanel TP = null;
        static readonly List<IMyCameraBlock> Camers = new List<IMyCameraBlock>();

        static PID Rul = new PID();
        static VirtualThrusts Trusts = new VirtualThrusts();
        static DirData AllThrusts = new DirData();

        static int CP = 0;
        static double Mass = 0, Height;
        static Vector3D speed = Vector3D.Zero, VGr;

        static bool aded = false;
        static string ss, mes;

        // ---------------------------------- MAIN --------------------------------------------- 
        void Main(string argument, UpdateType tpu)
        {
            try
            {
                if (tpu < UpdateType.Update1 && !string.IsNullOrEmpty(argument)) { SetAtributes(argument); return; }
                if (RemCon == null) { Echo("Необходима инициализация"); return; }
                var Tic = T.Run();
                if (Tic == 0 && T.TC % 320 != 0) return;

                VGr = RemCon.GetNaturalGravity();

                double gr = VGr.Length();
                if (gr > 0) RemCon.TryGetPlanetElevation(MyPlanetElevation.Surface, out Height);

                if (Tic > 0)
                {
                    Mass = RemCon.CalculateShipMass().TotalMass;
                    GetVec_S(RemCon.GetShipVelocities().LinearVelocity, out speed);
                }

                StringBuilder textout = new StringBuilder("Скор: " + speed.ToString("0.#") + " CP:" + CP);
                if (gr > 0) textout.AppendFormat(SYS, "\nВыс: {0:0.0}m. Гр: {1:0.00}g.", Height, gr);
                textout.AppendFormat(SYS, "_{0} {1}", Rul.KeyWait, Rul.Dir);

                if (Rul.Dir != PID.DIR.None)
                {
                    var Vdr = Rul.Rules();
                    textout.AppendFormat("\nYaw: {0:0.00} Pitch: {1:0.00} Roll: {2:0.00}", MathHelper.ToDegrees(Vdr.X), MathHelper.ToDegrees(Vdr.Y), MathHelper.ToDegrees(Vdr.Z));
                }

                if (Tic > 0 && CP != 0)
                {
                    MatrixD MV = new MatrixD();
                    /*0 - Гравитация
                      1 - Эфективная тяга
                      2 - Эфективная тяга учитывая гравитацию
                      3 - Время торможения
                      [0, 3] - Дистанция до цели*/

                    var stb = new StringBuilder();
                    if (gr > 0) { Vector3D tmp; GetVec_S(VGr, out tmp); MV.Right = tmp; }
                    PowS.GetPows(speed, ref MV);

                    int i = MV.Translation.AbsMaxComponent();
                    Base6Directions.Direction CurDirect = Base6Directions.GetBaseAxisDirection((Base6Directions.Axis)i);
                    if (speed.GetDim(i) < 0) CurDirect = Base6Directions.GetFlippedDirection(CurDirect);
                    double pow = MV[2, i];
                    if (pow > 0) stb.AppendFormat(SYS, "{3} Stop:{2:#,##0.0}м, {1:#,##0.#}с, P:{0:0.##}", pow, MV[3, i], PowS.GetDistStop(speed.GetDim(i), pow), CurDirect);
                    else stb.AppendFormat(SYS, "{0} Не хватает тяги для остановки {1:#,##0.0}", CurDirect, -pow);

                    if (!Rul.IsTarget()) stb.AppendLine();
                    else stb.AppendFormat(SYS, " >{0} {1}\n", (MV[0, 3] = Rul.Distance()).ToString("#,##0.0", SYS), Rul.Radius < 1 ? '*' : '@');

                    switch (CP)
                    {
                        case 3:// Установка тяги атмосферников
                            {
                                var AtmUp = Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].GetValues();

                                PowS.GetProcOverride((maxSpeed - speed.Y + gr) * Mass, AtmUp);
                                pow = Trusts[TrustsData.ThrustType.Thrust, Base6Directions.Direction.Backward].GetValues().EffectivePow;
                                if (AtmUp.EffectivePow < pow ||
                                    (AtmUp.EffectivePow < Mass * gr && AtmUp.EffectivePow < pow + Trusts[TrustsData.ThrustType.Hydrogen, Base6Directions.Direction.Backward].GetValues().EffectivePow))// Проверка скорости и эффективности ускорителей 
                                {
                                    stb.Append("Поворот...");
                                    CP = 0;//вперед и низ атм. - выкл 
                                    Rul.KeyWait = 5;
                                    Rul.Dir = PID.DIR.Forw | PID.DIR.NoGrav;
                                    Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].ForEach(x => x.ThrustOverride = 0);
                                    AllThrusts.ForEachToType(TrustsData.ThrustType.Thrust, x => x.Enabled = true);
                                    Trusts[0, Base6Directions.Direction.Forward].ForEach(x => x.Enabled = false);
                                }
                                else
                                {
                                    float coof = PowS.GetProcOverride((maxSpeed - speed.Y + gr) * Mass, AtmUp);
                                    Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].ForEach(x => x.ThrustOverridePercentage = coof);
                                    stb.Append($"Atm: {coof * 100:0.00}%");
                                }
                            }
                            break;
                        case 5: // Расчет тяги 
                            {
                                if (Vector3D.IsZero(VGr)) // Уже в космосе   
                                {
                                    Stoped(true);
                                    stb.Append("Выход из границ гравитации завершен!");
                                    SetAtributes(Rul.IsTarget() ? "avto" : "show");
                                    break;
                                }
                                var Trust_Forw = Trusts[TrustsData.ThrustType.Thrust, Base6Directions.Direction.Backward].GetValues();
                                double tgr = -MV[0, 0];
                                float coof = PowS.GetProcOverride((maxSpeed - speed.X + tgr) * Mass, Trust_Forw);
                                Trusts[TrustsData.ThrustType.Thrust, Base6Directions.Direction.Backward].ForEach(x => x.ThrustOverridePercentage = coof);
                                stb.Append($"Ion: {coof * 100:0.00}%");

                                if (coof <= 1) Trusts[TrustsData.ThrustType.Hydrogen, Base6Directions.Direction.Backward].ForEach(x => x.Enabled = false);// При нехватке тяги включаем или отключает гидротягу 
                                else
                                {
                                    coof = PowS.GetProcOverride((maxSpeed - speed.X + tgr) * Mass - Trust_Forw.CurrentPow, Trusts[TrustsData.ThrustType.Hydrogen, Base6Directions.Direction.Backward].GetValues());
                                    Trusts[TrustsData.ThrustType.Hydrogen, Base6Directions.Direction.Backward].ForEach(x => { x.ThrustOverridePercentage = coof; x.Enabled = true; });
                                    stb.Append($" Hydr: {coof * 100:0.00}%");

                                    if (coof > 1)
                                        if (speed.X > 0) stb.Append(" Не хватает тяги");
                                        else if (Trusts.GetSpecThrusts(0, Base6Directions.Direction.Backward).GetValues().EffectivePow < Math.Abs(Mass * MV.Right.X))
                                        { SetAtributes("down"); stb.Append("Взлет не возможен, падаем"); }
                                }
                            }
                            break;
                        case 6:
                        case 7:
                            {
                                if (gr == 0) { stb.Append("Ожидание входа в атмосферу"); break; }
                                var Atm_Up = Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].GetValues();
                                double timestop = Atm_Up.EffectivePow - Mass * gr;  // полезная тяга 

                                if (timestop < Mass / 4)
                                    stb.AppendFormat(SYS, "Тяги не хватает, коэф: {0:#,##0.00} мин. коэф: {1:#,##0.00}", Atm_Up.TecCoof, (Mass * gr) / Atm_Up.EffectivePow * Atm_Up.TecCoof);
                                else if (Atm_Up.TecCoof < 0.4)
                                    stb.AppendFormat(SYS, "Коэффициент эффективной тяги: {0:#,##0.00}, жду > 0,42", Atm_Up.TecCoof);
                                else
                                {
                                    double MaxHeight = PowS.GetNexSpeed(-speed.Z, MV.Right.Z),//будущая скорость 
                                    dist = (-speed.Z >= maxSpeed ? -speed.Z : (-speed.Z + MV.Right.Z / 2)); //растояние за след.сек. 
                                    MaxHeight *= (MaxHeight / timestop * Mass) / 2; //крит. высота.  
                                    timestop = MaxHeight / (Atm_Up.EffectivePow - Mass * MV.Right.Z) * Mass;// время торможения 

                                    stb.AppendFormat(SYS, "Крит выс: {0:#,##0.00}m. {1:#,##0.0}c. коэф {2:#,##0.00}",
                                        MaxHeight, timestop, Atm_Up.TecCoof);

                                    timestop = MaxHeight - (Height - dist - 50);
                                    if (timestop < 0) break;
                                    if (timestop > MaxHeight * 0.7) { Stoped(); break; }
                                    if (CP == 7) { Stoped(); SetAtributes("show"); break; }
                                    timestop /= dist; CP++;
                                    T.Interval -= (int)(timestop * 65 * 16);
                                    Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].ForEach(x => x.ThrustOverridePercentage = 90);
                                }
                            }
                            break;
                        case 9:// Управление
                        case 10:
                        case 11:// Контроль    
                            if (Rul.IsTarget())
                            {
                                var dist = MV[0, 3];
                                if (CP == 9) { CurDirect = Base6Directions.Direction.Forward; i = 0; }

                                pow = PowS.GetDistStop(speed.GetDim(i), MV[2, i]);
                                if (CP >= 10 && pow > dist && dist > speed.GetDim(i)) // не успеваем тормозить в режиме наблюдения
                                {
                                    Trusts.GetSpecThrusts(0, CurDirect, x => x.MaxEffectiveThrust > 0, null).ForEach(x => { x.Enabled = true; x.ThrustOverride = 0; });
                                    var ps = RemCon.GetPosition();
                                    var val = Rul.Target.Center - ps;
                                    var m = MatrixD.CreateFromAxisAngle(RemCon.WorldMatrix.Right, MathHelper.ToRadians(45));
                                    Vector3D.TransformNoProjection(ref val, ref m, out val);
                                    Rul.SetTarget(val += ps);
                                    Note(MyGPS.GPS("NewTarg", val));
                                    SetAtributes("avto");
                                    Rul.KeyWait = int.MaxValue;
                                    stb.Append(" Меняем курс");
                                }

                                if (dist < 1 && CP == 11) { SetAtributes("show"); break; }
                                pow = MV[0, i];
                                if (CP == 9) pow += Trusts.GetSpecThrusts(0, Base6Directions.GetFlippedDirection(CurDirect), x => x.Enabled).GetValues().EffectivePow / Mass;

                                if (Rul.IsTarget() && Rul.Radius < 1 && dist < 10000) stb.Append("Rescan: " + Rul.Scan(Rul.Center));

                                double nextspeed = PowS.GetNexSpeed(speed.GetDim(i), pow);
                                dist -= speed.GetDim(i) / 2 + nextspeed / 2; //дистанция до цели через секунду
                                pow = PowS.GetDistStop(nextspeed, MV[2, i]);

                                var Backward = Trusts.GetSpecThrusts(0, Base6Directions.GetFlippedDirection(CurDirect), x => x.Enabled, null);
                                stb.AppendFormat(SYS, "Next:{0:#,##0.0##}мс D.St:{2:#,##0.0}м D:{1:0.0}", nextspeed, dist, pow);

                                if (dist < pow) // тормозим
                                {
                                    if (CP != 11)
                                    {
                                        Backward.ForEach(x => x.ThrustOverride = 0);
                                        Trusts.GetSpecThrusts(0, CurDirect, x => x.Enabled, null).ForEach(x => x.ThrustOverride = 0);
                                        CP = 11;
                                    }
                                    stb.Append(" Торомозим");
                                }
                                else if (speed.X >= maxSpeed - 0.01 || dist < pow || CP == 11) // доп тяга не нужна 
                                {
                                    if (CP != 10)
                                    {
                                        Backward.ForEach(x => x.ThrustOverride = 0);
                                        stb.Append(" выкл тяги");
                                        Trusts.GetSpecThrusts(0, CurDirect, x => x.Enabled, null).ForEach(x => x.ThrustOverride = 0.000001f);
                                        CP = 10;
                                    }
                                    else stb.Append(" без тяги");
                                }
                                else
                                {
                                    float coof = PowS.GetProcOverride((Math.Min(maxSpeed - speed.X, MV[0, 3]) + MV[0, i]) * Mass, Backward.GetValues());
                                    Backward.ForEach(x => x.ThrustOverridePercentage = coof);
                                    stb.Append($" W:{ coof * 100:0.00}%");
                                }

                            }
                            break;
                        default: break;
                    }
                    ss = stb.ToString();
                }

                textout.Append("\n" + ss + "\n" + mes);
                Echo_(textout.ToString(), false);
                if (aded && Tic > 1 && TP != null) TP.CustomData += "\n\n" + textout.ToString();
            }
            catch (Exception e) { Echo_(e.ToString()); }
        }
        // ---------------------------------- end MAIN ---------------------------------------------       

        void SetAtributes(params string[] Args)
        {
            try
            {
                int Len = Args.GetLength(0);
                for (int i = 0; i < Len; i++)
                {
                    string Arg = Args[i];
                    if (Arg.Length == 0) continue;
                    int pos = Arg.IndexOf(':');
                    if (pos < 0) pos = Arg.Length;
                    string Right;
                    if (Arg.Length != pos)
                    {
                        Right = Arg.Substring(pos + 1);
                        Arg = Arg.Remove(pos);
                    }
                    else
                        Right = "";
                    Arg = Arg.ToLower();
                    switch (Arg)
                    {
                        case "targ":// Включение    
                            {
                                Vector3D v;
                                if (!MyGPS.TryParseVers(Right, out v))
                                    ShowAndStop("Не верный формат цели");
                                Rul.SetTarget(v);
                            }
                            break;
                        case "up":// Включение подьема   
                            {
                                if (RemCon == null) return; //Не инициализирован 
                                if (Trusts[TrustsData.ThrustType.Thrust, Base6Directions.Direction.Backward].Count == 0)
                                { ShowAndStop("Нет ионных ускорителей для выхода в космос"); return; }
                                if (Vector3D.IsZero(RemCon.GetNaturalGravity()))
                                { ShowAndStop("Нет гравитации"); return; }

                                if (!string.IsNullOrWhiteSpace(Right))
                                {
                                    Vector3D v;
                                    if (!MyGPS.TryParseVers(Right, out v)) { ShowAndStop("Не верный формат цели"); return; }
                                    Rul.SetTarget(v);
                                }
                                Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].ForEach(x => x.Enabled = true);
                                T.SetInterval(960, UpdateFrequency.Update10, true);
                                CP = 1;
                                Rul.Dir = PID.DIR.Up;
                                Rul.KeyWait = 3;
                            }
                            break;
                        case "down":// Включение спуска 
                            {
                                if (RemCon == null) return; //Не инициализирован 
                                T.SetInterval(960, UpdateFrequency.Update10, true);
                                CP = 6;
                                Rul.Dir = PID.DIR.Up | PID.DIR.IgnorTarget;
                                AllThrusts.ForEach(x => x.Enabled = false);
                            }
                            break;
                        case "avto":
                        case "show":// вывод инфы 
                            {
                                if (RemCon == null || !Rul.IsTarget()) return; //Не инициализирован 
                                T.SetInterval(960, UpdateFrequency.Update10, true);
                                Rul.Dir = PID.DIR.Forw;
                                if (Arg == "avto") { Rul.KeyWait = 9; Trusts.Clear(); return; }
                                CP = 8; Rul.Dir |= PID.DIR.NoRules;
                                Rul.ForEach(x => x.GyroOverride = false);
                            }
                            break;
                        case "-":
                        case "off": // Отключение 
                            Stoped(true);
                            break;
                        case "distin":
                            {
                                Vector3D nv;
                                if (!MyGPS.TryParseVers(Right, out nv)) { ShowAndStop("Не верный формат цели"); return; }
                                var kam = Camers.Find(x => x.CanScan(nv));
                                if (kam == null)
                                {
                                    if (MyMath.AngleBetween(RemCon.WorldMatrix.Forward, nv - RemCon.GetPosition()) < MathHelper.PiOver4)
                                        ShowAndStop("Нет камеры способной на это. Растояние: " + (RemCon.GetPosition() - nv).Length());
                                    else
                                    {
                                        Rul.SetTarget(nv, 1);
                                        SetAtributes("avto"); ss = "Поворот на цель";
                                        Rul.KeyWait = int.MaxValue; CP = 0;
                                    }
                                    return;
                                }
                                var inf = kam.Raycast(nv);
                                if (inf.IsEmpty()) { Rul.SetTarget(nv, 1); ShowAndStop("Object not found"); return; }
                                else { Rul.SetTarget(inf.BoundingBox); SetAtributes("avto"); }
                            }
                            break;
                        case "dist":
                            {
                                Stoped();
                                IMyCameraBlock kam = new Selection("*").FindBlock<IMyCameraBlock>(GridTerminalSystem, x => x.IsActive);
                                if (kam == null) { ShowAndStop($"Не установвлена текущая камера " + Right); return; }
                                int dist = 20000;
                                if (!string.IsNullOrWhiteSpace(Right))
                                    if (!int.TryParse(Right, out dist)) { ShowAndStop("Параметр дистанции не верный:" + Right); return; }
                                    else Echo("Max dist: " + dist);
                                if (!kam.EnableRaycast)
                                {
                                    kam.EnableRaycast = true;
                                    ShowAndStop("" + kam.TimeUntilScan(dist) + "ms.   " + kam.RaycastConeLimit);
                                    return;
                                }
                                if (!kam.CanScan(dist)) { ShowAndStop(kam.TimeUntilScan(dist).ToString() + "ms."); return; }
                                var inf = kam.Raycast(dist);
                                if (inf.IsEmpty()) { Rul.SetTarget(Vector3D.Zero); ShowAndStop("Target not found"); return; }
                                Rul.SetTarget(inf.BoundingBox);
                                Note(MyGPS.GPS(inf.Name + " R-" + Rul.Radius.ToString("0"), inf.Position));
                                var s = $"Найлена цель: {inf.Name} dist: {Rul.Distance().ToString("#,##0.0", SYS)}";
                                Echo_(s); if (TP != null) Echo(s);
                            }
                            break;
                        case "dist?":
                            {
                                Stoped();
                                IMyCameraBlock kam = new Selection("*").FindBlock<IMyCameraBlock>(GridTerminalSystem, x => x.IsActive);
                                if (kam == null) { ShowAndStop($"Не установвлена текущая камера " + Right); return; }
                                int dist = 20000;
                                if (!string.IsNullOrWhiteSpace(Right))
                                    if (!int.TryParse(Right, out dist)) { ShowAndStop("Параметр дистанции не верный:" + Right); return; }
                                    else Echo("Max dist: " + dist);
                                if (!kam.EnableRaycast)
                                {
                                    kam.EnableRaycast = true;
                                    ShowAndStop("" + kam.TimeUntilScan(dist) + "ms.   " + kam.RaycastConeLimit);
                                    return;
                                }
                                if (!kam.CanScan(dist)) { ShowAndStop(kam.TimeUntilScan(dist).ToString() + "ms."); return; }
                                var inf = kam.Raycast(dist);
                                if (inf.IsEmpty()) { Rul.SetTarget(Vector3D.Zero); ShowAndStop("Target not found"); return; }

                                Rul.SetTarget(inf.BoundingBox);
                                var s = new StringBuilder(MyGPS.GPS(inf.Name + " R-" + Rul.Radius.ToString("0.0"), inf.Position) + "\n");
                                s.AppendLine(inf.BoundingBox.ToString());
                                s.AppendLine(inf.BoundingBox.Size.ToString());
                                //s.AppendLine($"{Rul.Target.Distance(l.Position)} - {Rul.Radius} / {Rul.Distance()}");
                                Echo_(s.ToString());
                                if (TP != null) Echo(s.ToString());
                            }
                            break;
                        case "hist": { if (TP != null && (aded = !aded)) TP.CustomData = string.Empty; } break;
                        case "?":
                            Echo_(">>>>>>>>>>>>");

                            if (Rul.IsTarget()) Echo_(MyGPS.GPS("Target " + Rul.Radius, Rul.Center));

                            /*Echo_("WM " + RemCon.WorldMatrix);
                            Echo_("AB " + RemCon.WorldAABB);
                            Echo_("ABHr " + RemCon.WorldAABBHr);

                            Echo_("Pos " + RemCon.GetPosition());
                            Echo_("Gr " + VGr);
                            Echo_("Sp " + RemCon.GetShipVelocities().LinearVelocity);*/

                            ShowAndStop("Инфа выведена");
                            break;
                        case "test":
                            {
                                Rul.ForEach(x => x.GyroOverride = false);
                                SetAtributes("show"); Rul.Dir = PID.DIR.NoRules | PID.DIR.Up;
                            }
                            break;
                        default:
                            {
                                switch (Arg)
                                {
                                    case "Init": Init(Right); break;
                                    default: ShowAndStop("Левая команда: " + Arg); break;
                                }
                            }
                            break;
                    }
                }
            }
            catch (Exception e) { Echo_(e.ToString()); }
        }

        void Stoped(bool clearTrust = false)
        {
            T.Stop();
            CP = -1;
            Rul.Dir = PID.DIR.None;

            Rul.ForEach(x => x.GyroOverride = false);
            AllThrusts.ForEachToType(TrustsData.ThrustType.Hydrogen, x => { x.ThrustOverride = 0; x.Enabled = false; });
            bool kosmos = Vector3D.IsZero(RemCon.GetNaturalGravity());
            if (!kosmos) kosmos = Trusts[TrustsData.ThrustType.Atmospheric, Base6Directions.Direction.Down].GetValues().EffectivePow <
                     Trusts[TrustsData.ThrustType.Thrust, Base6Directions.Direction.Down].GetValues().EffectivePow;

            AllThrusts.ForEachToType(TrustsData.ThrustType.Atmospheric, x => { x.ThrustOverride = 0; x.Enabled = !kosmos; });
            AllThrusts.ForEachToType(TrustsData.ThrustType.Thrust, x => { x.ThrustOverride = 0; x.Enabled = kosmos; });

            if (clearTrust) Trusts.Clear();
        }

        void ShowAndStop(string text) { if (T.Interval != 0) Stoped(); Echo_(text); }

        void Init(string panel)
        {
            TP = new Selection(panel).FindBlock<IMyTextPanel>(GridTerminalSystem);
            if (TP != null) { TP.ShowPublicTextOnScreen(); TP.WritePublicText(""); }
            RemCon = new Selection("").FindBlock<IMyShipController>(GridTerminalSystem);
            if (RemCon == null) { Echo("Блок управления не найден"); return; }

            var TrL = new List<IMyThrust>();
            new Selection(null).FindBlocks<IMyThrust>(GridTerminalSystem, TrL);
            if (TrL.Count == 0) { Echo("Не найдены трастеры"); return; }

            AllThrusts.ForEach((x, y) => x.Clear());
            TrL.ForEach(x =>
            {
                if (!x.IsFunctional)
                { Echo(x.CustomName + " не функционирует"); return; }
                else
                    AllThrusts[Base6Directions.GetClosestDirection(x.GridThrustDirection)].
                        Add(x, TrustsData.GetTypeFromSubtypeName(x.BlockDefinition.SubtypeName));
            });

            var gl = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(Rul);
            Echo("Найдено " + Rul.Count + " гироскопов");
            Echo("Инициализация завершена");
        }

        void Echo_(string mask, params object[] vals) => Echo_(string.Format(mask, vals));
        void Echo_(string text, bool append = true) { if (TP == null) Echo(text); else TP.WritePublicText(text + "\n", append); }
        void Note(string txt, bool unucal = true)
        {
            var str = RemCon.CustomData;
            if (unucal && str.IndexOf(txt) >= 0) return;
            RemCon.CustomData += !string.IsNullOrWhiteSpace(str) ? "\n" + txt : txt;
        }


        public static void GetVec_S(Vector3D sp, out Vector3D ouv)
        {
            var rd = Vector3D.Reflect(sp, RemCon.WorldMatrix.Down);
            ouv.X = Vector3D.Reflect(rd, RemCon.WorldMatrix.Left).Dot(RemCon.WorldMatrix.Forward);
            ouv.Y = Vector3D.Reflect(rd, RemCon.WorldMatrix.Forward).Dot(RemCon.WorldMatrix.Right);
            ouv.Z = Vector3D.Reflect(Vector3D.Reflect(sp, RemCon.WorldMatrix.Left), RemCon.WorldMatrix.Forward).Dot(RemCon.WorldMatrix.Up);
        }

        class PowS
        {
            public static void GetPows(Vector3D speed, ref MatrixD M)
            {
                Base6Directions.Direction i;
                for (byte j = 0; j < 3; j++)
                {
                    i = Base6Directions.GetBaseAxisDirection((Base6Directions.Axis)j);
                    if (speed.GetDim(j) < 0) { i++; M[0, j] = -M[0, j]; }
                    M[1, j] = Trusts.GetSpecThrusts(0, i, x => x.Enabled, null).GetValues().EffectivePow / Mass;
                    M[2, j] = M[1, j] - M[0, j];
                    M[3, j] = Math.Abs(speed.GetDim(j)) / M[2, j];
                }
            }

            public static double GetPowsOne(Base6Directions.Direction Ax, double gr)
                => Trusts.GetSpecThrusts((byte)Ax, Ax, x => x.Enabled, null).GetValues().EffectivePow / Mass - gr;

            public static double GetDistStop(double speed, double powStop) => Math.Pow(speed, 2) / powStop / 2;
            public static float GetProcOverride(double mass, TrustsData.ThrustsValue Thrust)
            => (float)(mass / Thrust.Max_pow / Thrust.TecCoof);

            public static double GetNexSpeed(double tecSpeed, double Pow)
            {
                var tmp = tecSpeed + Pow;
                if (tmp > maxSpeed) tmp = Math.Max(tecSpeed, maxSpeed);
                return tmp;
            }
        }


        public class TrustsData : List<IMyThrust>
        {
            public TrustsData(ThrustType tp) { Type = tp; }
            public struct ThrustsValue
            {
                public double Max_pow, EffectivePow, CurrentPow;
                public void Add(IMyThrust val) { Max_pow += val.MaxThrust; EffectivePow += val.MaxEffectiveThrust; CurrentPow += val.CurrentThrust; }
                public void Clear() { Max_pow = 0; EffectivePow = 0; CurrentPow = 0; }
                public double TecCoof { get { return EffectivePow / Max_pow; } }
                public new string ToString() => $"M:{Max_pow} E:{EffectivePow}, C:{CurrentPow}";
            }
            [Flags] public enum ThrustType : byte { Small = 1, Large, Atmospheric = 4, Hydrogen = 8, Thrust = 16 };
            public readonly ThrustType Type;

            public virtual ThrustsValue GetValues()
            {
                var res = new ThrustsValue();
                ForEach(x => res.Add(x));
                return res;
            }
            public static ThrustType GetTypeFromSubtypeName(string type)
            {
                var rst = type.Substring(10).Split('T', 'H', 'A');
                ThrustType res = rst[0] == "Large" ? ThrustType.Large : ThrustType.Small;
                if (rst.Length == 3) res |= (rst[1] == "ydrogen" ? ThrustType.Hydrogen : ThrustType.Atmospheric);
                else res |= ThrustType.Thrust;
                return res;
            }
        }

        public class TrustsDatas : List<TrustsData>
        {
            public static readonly TrustsDatas Empty = new TrustsDatas();
            public void Add(IMyThrust val, TrustsData.ThrustType Type)
            {
                var tmp = Find(x => x.Type == Type);
                if (tmp == null) Add(new TrustsData(Type) { val });
                else tmp.Add(val);
            }
            public void ForEach(Action<IMyThrust> Act) => ForEach(y => y.ForEach(x => Act(x)));
            public void ForEachIf(Action<IMyThrust> Act, Func<IMyThrust, bool> TrIf = null, Func<TrustsData, bool> GrIf = null) =>
                ForEach(y => { if (GrIf == null || GrIf(y)) y.ForEach(x => { if (TrIf == null || TrIf(x)) Act(x); }); });
            public void CopyTrusts(TrustsData.ThrustType Type, TrustsDatas res, bool absType = false)
            { foreach (var x in this) if ((absType ? x.Type : x.Type & Type) == Type) res.Add(x); }

            public virtual TrustsData.ThrustsValue GetValues()
            {
                var res = new TrustsData.ThrustsValue();
                ForEach(y => y.ForEach(x => res.Add(x)));
                return res;
            }
        }

        public class BufTrustsData : List<IMyThrust>
        {
            TrustsData.ThrustsValue TecVal;
            double LastHeight = -1;
            public TrustsData.ThrustsValue GetValues()
            {
                if (LastHeight == Height) return TecVal;
                TecVal.Clear();
                ForEach(x => TecVal.Add(x));
                LastHeight = Height;
                return TecVal;
            }
        }
        public class VirtualThrusts : Dictionary<int, BufTrustsData>
        {
            public BufTrustsData GetSpecThrusts(byte key, Base6Directions.Direction dir, Func<IMyThrust, bool> TrIf = null, Func<TrustsData, bool> GrIf = null)
            {
                if (key == 0) key = (byte)dir;
                key += 200;
                BufTrustsData res;
                if (!TryGetValue(key, out res))
                { res = new BufTrustsData(); AllThrusts[dir].ForEachIf(x => res.Add(x), TrIf, GrIf); Add(key, res); }
                return res;
            }
            BufTrustsData GetThrusts(TrustsData.ThrustType type, Base6Directions.Direction dir)
            {
                var key = GetKey(type, dir);
                BufTrustsData res;
                if (!TryGetValue(key, out res))
                { res = new BufTrustsData(); AllThrusts[dir].ForEachIf(x => res.Add(x), null, x => (x.Type & type) == type); Add(key, res); }
                return res;
            }
            public BufTrustsData this[TrustsData.ThrustType type, Base6Directions.Direction dir] { get { return GetThrusts(type, dir); } }
            public static int GetKey(TrustsData.ThrustType type, Base6Directions.Direction dir) => ((byte)dir + 1) * 20 + (byte)type;
        }


        public class DirData : List<TrustsDatas>
        {
            public DirData() : base(6) { for (var i = 0; i < 6; i++) base.Add(new TrustsDatas()); }
            public void ForEach(Action<IMyThrust> Act) { for (var i = 0; i < 6; i++) this[i].ForEach(x => Act(x)); }
            internal void ForEach(Action<TrustsDatas, Base6Directions.Direction> Act)
            { foreach (Base6Directions.Direction i in Enum.GetValues(typeof(Base6Directions.Direction))) Act(base[(int)i], i); }
            public void ForEachToType(TrustsData.ThrustType Type, Action<IMyThrust> Act, bool absType = false)
            { for (var i = 0; i < 6; i++) this[i].ForEachIf(x => Act(x), null, x => absType ? x.Type == Type : (x.Type & Type) != 0); }
            public TrustsDatas this[Base6Directions.Direction i] { get { return base[(int)i]; } }

        }

        public class PID : List<IMyGyro>
        {
            [Flags] public enum DIR { None, NoRules, NoGrav, IgnorTarget = 4, Forw = 8, Back = 16, Up = 32 };
            public DIR Dir = DIR.None;
            public BoundingBoxD Target { get; protected set; }
            public bool IsTarget() => !Vector3D.IsZero(Target.Max);
            public Vector3D Center { get { return Target.Center; } }
            public BoundingSphereD SphereD() => BoundingSphereD.CreateFromBoundingBox(Target);
            public double Radius { get; protected set; }


            public int KeyWait = 0;
            public double Distance() => Target.Distance(RemCon.GetPosition());

            public void SetTarget(BoundingBoxD val) { Target = val; Radius = Target.Size.Length() / 2; }
            public void SetTarget(Vector3D val, double dist = 0)
            {
                if (Vector3D.IsZero(val)) { Target = new BoundingBoxD(val, val); Radius = 0; }
                else SetTarget(BoundingBoxD.CreateFromSphere(new BoundingSphereD(val, Math.Max(dist, 0.01))));
            }
            public byte Scan(Vector3D val)
            {
                var kam = Camers.Find(x => x.CanScan(val));
                if (kam == null) return 0;
                var inf = kam.Raycast(val);
                if (!inf.IsEmpty()) { SetTarget(inf.BoundingBox); return 2; }
                return 1;
            }


            public Vector3 Rules()//(DIR Dir = DIR.None)
            {
                //if (Dir == DIR.None) Dir = this.Dir;
                Vector3 Vdr = Vector3D.Zero;
                var isGr = !Vector3D.IsZero(VGr);
                var istrg = IsTarget() && (Dir & DIR.IgnorTarget) != DIR.IgnorTarget;
                if (!isGr && !istrg) return Vdr;

                if ((Dir & DIR.Forw) == DIR.Forw)
                {
                    Vector3D vNap = istrg ? Center - RemCon.GetPosition() : -VGr;
                    if (isGr && (Dir & DIR.NoGrav) != DIR.NoGrav) vNap = Vector3D.Reject(vNap, Vector3D.Normalize(VGr));

                    Vdr.X = GetAngel(RemCon.WorldMatrix.Down, RemCon.WorldMatrix.Forward, vNap);
                    Vdr.Y = GetAngel(RemCon.WorldMatrix.Right, RemCon.WorldMatrix.Forward, vNap);
                    if (isGr) Vdr.Z = GetAngel(RemCon.WorldMatrix.Forward, RemCon.WorldMatrix.Down, VGr);
                }
                else if ((Dir & DIR.Up) == DIR.Up)
                {
                    Vector3D vNap = istrg ? Center - RemCon.GetPosition() : -VGr;

                    Vdr.Z = GetAngel(RemCon.WorldMatrix.Forward, RemCon.WorldMatrix.Down, VGr);
                    Vdr.Y = GetAngel(RemCon.WorldMatrix.Right, RemCon.WorldMatrix.Up, vNap);
                }

                if ((Dir & DIR.NoRules) != DIR.NoRules)
                {
                    var abs = Vector3D.Abs(Vdr);
                    if (KeyWait != 0 && abs.X < 0.5 && abs.Y < 0.5 && abs.Z < 0.5)
                    {
                        if (KeyWait != int.MaxValue) CP = KeyWait;
                        else if (Scan(Center) > 0) CP = 9;
                        else CP = 8;
                        KeyWait = 0;
                    }

                    if (abs.X < 0.004f) Vdr.X = 0;
                    if (abs.Y < 0.004f) Vdr.Y = 0;
                    if (abs.Z < 0.004f) Vdr.Z = 0;
                    if (Vdr.X != 0 || Vdr.Y != 0 || Vdr.Z != 0) Drive(Vdr.X, Vdr.Y, Vdr.Z, RemCon.WorldMatrix);
                    else ForEach(x => x.GyroOverride = false);
                }

                return Vdr;
            }
            public void Drive(double yaw_speed, double pitch_speed, double roll_speed, MatrixD shipMatrix)
            {
                if (Count == 0) return;
                var relativeRotationVec = Vector3D.TransformNormal(new Vector3D(-pitch_speed, yaw_speed, roll_speed), shipMatrix);
                foreach (var thisGyro in this)
                {
                    var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
                    thisGyro.GyroOverride = true;
                    // mes = string.Format("{0:0.000} {1:0.000} {2:0.000} {3}", thisGyro.Yaw, thisGyro.Pitch, thisGyro.Roll, transformedRotationVec.ToString("0.000"));
                    thisGyro.Pitch = (float)transformedRotationVec.X - (transformedRotationVec.X > 0.5 ? thisGyro.Pitch / 3 : 0);
                    thisGyro.Yaw = (float)transformedRotationVec.Y - (transformedRotationVec.Y > 0.5 ? thisGyro.Yaw / 3 : 0);
                    thisGyro.Roll = (float)transformedRotationVec.Z - (transformedRotationVec.Z > 0.5 ? thisGyro.Roll / 3 : 0);
                }
            }

            /// <summary> 
            ///  Расчитывает угол поворота 
            /// </summary> 
            /// <param name="Pl">Плоскость</param> 
            /// <param name="VDirect">Вектор поворота</param> 
            /// <param name="Targ">Вектор цели</param> 
            public static float GetAngel(Vector3D Pl, Vector3D VDirect, Vector3D Targ)
            {
                var tm = Vector3D.Reject(Targ, Pl);
                var u = Math.Acos(VDirect.Dot(tm) / (VDirect.Length() * tm.Length()));
                return (float)(MyMath.AngleBetween(tm, Pl.Cross(VDirect)) > MathHelper.PiOver2 ? -u : u);
            }

            public string ToSave() => string.Format("{0},{1},{2}", KeyWait, Dir, string.Join(";", Target.GetCorners()));
            public void Load(string val)
            {
                var vl = val.Split(',');
                KeyWait = int.Parse(vl[0]);
                Rul.Dir = (PID.DIR)Enum.Parse(typeof(PID.DIR), vl[1]);
                vl = vl[2].Split(';');
                var lst = new List<Vector3D>(vl.Length);
                for (var i = 0; i < vl.Length; i++)
                { Vector3D tm; Vector3D.TryParse(vl[i], out tm); lst.Add(tm); }
                SetTarget(BoundingBoxD.CreateFromPoints(lst));
            }

        }


        public class Selection
        {
            byte key = 0; string Val;
            public bool inv; public IMyCubeGrid GR = null;
            public string Value { get { return Val; } set { SetSel(value); } }

            public Selection(string val, IMyCubeGrid grid = null) { Value = val; GR = grid; }
            public Selection Change(string val) { Value = val; return this; }
            void SetSel(string val)
            {
                Val = ""; key = 0;
                if (string.IsNullOrEmpty(val)) return;
                inv = val.StartsWith("!"); if (inv) val = val.Remove(0, 1);
                if (string.IsNullOrEmpty(val)) return;
                int Pos = val.IndexOf('*', 0, 1) + val.LastIndexOf('*', val.Length - 1, 1) + 2;
                if (Pos == 0) Pos = 0;
                else if (Pos == 1) Pos = 1;
                else if (Pos == val.Length) Pos = 2;
                else Pos = Pos < 4 ? 1 : 3;
                if (Pos != 0)
                {
                    if (Pos != 2) val = val.Remove(0, 1);
                    if (Pos != 1) val = val.Remove(val.Length - 1, 1);
                }
                Val = val; key = (byte)Pos;
            }
            public override string ToString() { return inv ? "!" : ""; }
            public bool Complies(IMyTerminalBlock val)
            {
                if (GR != null && val.CubeGrid != GR) return false;
                return Complies(val.CustomName);
            }
            public bool Complies(string str)
            {
                if (string.IsNullOrEmpty(Val)) return !inv;
                switch (key)
                {
                    case 0: return str == Val != inv;
                    case 1: return str.EndsWith(Val) != inv;
                    case 2: return str.StartsWith(Val) != inv;
                    case 3: return str.Contains(Val) != inv;
                }
                return false;
            }

            public Type FindBlock<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class
            {
                List<Type> res = new List<Type>(); bool fs = false;
                TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies(x as IMyTerminalBlock) && (Fp == null || Fp(x))));
                return res.Count == 0 ? null : res[0];
            }
            public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null)
            {
                TB.SearchBlocksOfName(inv ? "" : Val, res, x => Complies(x) && (Fp == null || Fp(x)));
            }
            public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
            { TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock)) && (Fp == null || Fp(x))); }
        }
        class Timer
        {
            readonly MyGridProgram GP;
            int Int;
            public bool zeroing;
            public int TC { get; protected set; }
            public Timer(MyGridProgram Owner, int Inter = 0, int tc = 0, bool zer = false)
            {
                GP = Owner;
                if (Inter == 0) zeroing = zer;
                else
                {
                    var P = new Point(Inter, 0);
                    CallFrequency(ref P);
                    SetInterval(P, zer);
                }
                TC = tc;
            }
            public int Interval
            {
                get { return Int; }
                set { SetInterval(RoundInt(value), zeroing); }
            }
            public void Stop() { GP.Runtime.UpdateFrequency = UpdateFrequency.None; Int = 0; }
            public static Point RoundInt(int value)
            {
                if (value == 0) return new Point(0, 0);
                Point v = new Point(value, 0);
                var del = CallFrequency(ref v);
                v.X = value % del;
                if (v.X > del / 2) value += del;
                v.X = value - v.X;
                return v;
            }
            public static int CallFrequency(ref Point res)
            {
                int del;
                if (res.X <= 960) { del = 16; res.Y = 1; }
                else if (res.X < 4000) { del = 160; res.Y = 2; }
                else { del = 1600; res.Y = 4; }
                return del;
            }
            public void SetInterval(int value, UpdateFrequency updateFreq, bool zeroing)
            { this.zeroing = zeroing; Int = value; GP.Runtime.UpdateFrequency = updateFreq; TC = 0; }
            public void SetInterval(Point val, bool zeroing)
            { this.zeroing = zeroing; Int = val.X; GP.Runtime.UpdateFrequency = (UpdateFrequency)val.Y; TC = 0; }
            public void SetInterval(int value, bool zeroing) { this.zeroing = zeroing; Interval = value; }
            public int Run()
            {
                if (Int == 0) return 1;
                TC += (int)GP.Runtime.TimeSinceLastRun.TotalMilliseconds;
                if (TC < Int) return 0;
                int res = TC;
                TC = zeroing ? 0 : TC % Int;
                return res;
            }
            public override string ToString() { return Int == 0 ? "отключено" : (Int + "мс:" + GP.Runtime.UpdateFrequency); }
            public string ToString(int okr) => Int == 0 ? "отключено" : GetInterval().ToString("##0.##");
            public double GetInterval(int okr = 1000)
            {
                if (Int == 0) return 0;
                if (!zeroing) return (double)Int / okr;
                int i;
                switch (GP.Runtime.UpdateFrequency)
                {
                    case UpdateFrequency.Update1: i = 16; break;
                    case UpdateFrequency.Update10: i = 160; break;
                    case UpdateFrequency.Update100: i = 1600; break;
                    default: return -1;
                }
                var b = Int % i;
                b = b == 0 ? Int : (Int / i + 1) * i;
                return ((double)b / okr);
            }
            public string ToSave() => $"{TC}@{zeroing}@{Int}";
            public static Timer Parse(string sv, MyGridProgram gp)
            {
                var s = sv.Split('@');
                return new Timer(gp, int.Parse(s[2]), int.Parse(s[0]), bool.Parse(s[1]));
            }
        }

        public static class MyGPS
        {
            public static string GPS(string Name, Vector3D Val)
            { return string.Format("GPS:{0}:{1:0.##}:{2:0.##}:{3:0.##}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
            public static string GPS(string Name, Vector3D Direct, Vector3D Pos, double dist = 0, string format = "")
            {
                Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
                return string.Format("GPS:{0}:{1}:{2}:{3}:",
                    Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format));
            }
            public static string Vec_GPS(string Name, Vector3D Direct, Vector3D Pos, double dist, string format = "")
            {
                Pos += (dist == 0 ? Direct : Vector3D.Normalize(Direct) * dist);
                return string.Format("{0}:{4}\nGPS:{0}:{1}:{2}:{3}:",
                  Name, Pos.GetDim(0).ToString(format), Pos.GetDim(1).ToString(format), Pos.GetDim(2).ToString(format), Direct.ToString(format));
            }
            public static bool TryParse(string vector, out Vector3D res)
            {
                var p = vector.Split(':');
                if (p.GetLength(0) == 6)
                {
                    float x, y, z;
                    if (float.TryParse(p[2], out x) && float.TryParse(p[3], out y) && float.TryParse(p[4], out z))
                        res = new Vector3D(x, y, z);
                    else
                        res = Vector3D.Zero;
                }
                else res = Vector3D.Zero;
                return res != Vector3D.Zero;
            }
            public static bool TryParseVers(string vector, out Vector3D res)
            {
                if (!vector.StartsWith("{")) return TryParse(vector, out res);
                return Vector3D.TryParse(vector, out res);
            }
        }


    }
}