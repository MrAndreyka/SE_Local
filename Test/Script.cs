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
    IMyTextPanel Txt = null;
    Camers camers = new Camers();
    static PID Rul = new PID();
    static IMyShipController RemCon = null;
    static MyDetectedEntityInfo Target;
    static Vector3D TarPos = Vector3D.Zero;


    Program()
    {
        RemCon = new Selection(null).FindBlock<IMyShipController>(GridTerminalSystem);
        if (RemCon == null) throw new Exception("Не найден блок управления");

        new Selection(null).FindBlocks<IMyCameraBlock>(GridTerminalSystem, camers);
        camers.ForEach(x => x.EnableRaycast = true);
        
        timer = new Timer(this);
        GridTerminalSystem.GetBlocksOfType<IMyGyro>(Rul);
        Rul.ForEach(x => x.GyroOverride = true);

        Txt = new Selection(null).FindBlock<IMyTextPanel>(GridTerminalSystem);

        Echo("Инициализировано");
    }


    void Main(string arg, UpdateType UT)
    {
        try
        {
            if (UT == UpdateType.Antenna) { NewTarget(arg); return; }
            else if (UT < UpdateType.Update1 && !string.IsNullOrWhiteSpace(arg)) { SetAtributes(arg); return; }

            if (Target.IsEmpty()) { timer.Stop(); return; }

            var TarPos = Target.Position + Target.Velocity * Target.TimeStamp / 1000;
            var cam = camers.GetCamera(TarPos);
            if (cam != null)
            {
                Target = cam.Raycast(TarPos);
                if (Target.Type != MyDetectedEntityType.LargeGrid && Target.Type != MyDetectedEntityType.SmallGrid)
                    Target = new MyDetectedEntityInfo();
                if (Target.IsEmpty()) { timer.Stop(); return; }
            }

            var Vdr = Rul.Rules();
            Mess(string.Format("\nYaw: {0:0.00} Pitch: {1:0.00} Roll: {2:0.00}", MathHelper.ToDegrees(Vdr.X), MathHelper.ToDegrees(Vdr.Y), MathHelper.ToDegrees(Vdr.Z)));

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
                if (ShowInfo) Mess(Target.Type == MyDetectedEntityType.None? "Not found": MyGPS.GPS(Target.Name, Target.Position));
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

    void NewTarget(string arg)
    {
        var ars = arg.Split(':');
        if (ars.Length < 2) return;

        if (ars[0] == "KillFree") { if (!Target.IsEmpty()) return; }
        else if (ars[0] != "Kill") { Mess("Проигнорирована команда: " + arg); return; }
        Vector3D tmp;
        Vector3D.TryParse(ars[1], out tmp);

        SetTarget(tmp);
        Txt.CustomData += "\n" + tmp;
    }

     void SetTarget(Vector3D Targ)
    {
        if (Targ == Vector3D.Zero) return;
        var cam = camers.GetCamera(Targ);
        if (cam == null) { Txt.CustomData += "\nНет камеры для инициализации"; return; }
        Target = cam.Raycast(Targ);
        if (Target.Type != MyDetectedEntityType.LargeGrid && Target.Type != MyDetectedEntityType.SmallGrid)
            Target = new MyDetectedEntityInfo();
        if (Target.IsEmpty()) { timer.Stop(); return; }
        else timer.SetInterval(960, true);
    }

    public void Mess(string text, bool consol = false)
    {
        if (consol || Txt == null) Echo(text);
        if (Txt != null) Txt.WritePublicText(text+"\n", true);
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

    public class Camers : List<IMyCameraBlock>
    {
        int tec = 0;
        public IMyCameraBlock GetCamera(Vector3D Pos)
        {
            if (Count == 0) return null;
            if (!this[tec].CanScan(Pos)) return null;
            var tc = this[tec];
            if (!tc.IsWorking) { Remove(tc); return GetCamera(Pos); }
            else if (++tec == Count) tec = 0;
            return tc;
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
