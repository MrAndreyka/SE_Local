/*----------------------------------------------------------------------  
    AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com  
    When using and disseminating information about the authorship is obligatory  
    При использовании и распространении информация об авторстве обязательна  
    ----------------------------------------------------------------------*/

    IMyProgrammableBlock Pb;
	IMyTextPanel Txt = null;

    Program()
    {
        string s = Me.CustomData;
        if (string.IsNullOrWhiteSpace(s)) s = Me.TerminalRunArgument;
        //if(string.IsNullOrWhiteSpace(s)) throw new Exception("Ожидаются параметры в CustomData");
        var ars = s.Split(';');

        if (ars.Length > 0)
        {
            Pb = new Selection(ars[0]).FindBlock<IMyProgrammableBlock>(GridTerminalSystem);
            if (Pb == null) Echo($"Не найден програмный блок \"{ars[0]}\"");
        }

        if (ars.Length > 1)
        {
            Txt = new Selection(ars[1]).FindBlock<IMyTextPanel>(GridTerminalSystem);
            if (Txt == null) Echo($"Не найдена панель \"{ars[1]}\"");
        }

        List<IMyCameraBlock> cams = new List<IMyCameraBlock>();
        new Selection(null).FindBlocks<IMyCameraBlock>(GridTerminalSystem, cams);
        cams.ForEach(x => x.EnableRaycast = true);

        Echo("Инициализировано");
    }

 
    void Main(string arg, UpdateType UT)
    {
        // if (UT < UpdateType.Update1 && !string.IsNullOrWhiteSpace(arg)) { SetAtributes(arg); return; }

        try
        {
            if (Txt != null) Txt.WritePublicText("");
            var cam = new Selection(null).FindBlock<IMyCameraBlock>(GridTerminalSystem, x => x.IsActive);
            if (cam == null) { Mess("Активная камера не найдена"); return; }

            int Dist = 10000;
            if (string.IsNullOrWhiteSpace(arg)) if (!int.TryParse(arg, out Dist)) Dist = 10000;
            Mess(MyGPS.GPS("Vector", cam.WorldMatrix.Forward, cam.GetPosition(), Dist));

            var Enty = cam.Raycast(Dist);
            if (Enty.Type != MyDetectedEntityType.SmallGrid && Enty.Type != MyDetectedEntityType.LargeGrid) { Mess("Цель не найдена"); return; }

            Mess(string.Format("{0} ({1}) {2}\n{3}", Enty.Name, Enty.Type, Enty.Position, MyGPS.GPS("Цель", Enty.Position)), true);
            var speek = new Selection(null).FindBlock<IMySoundBlock>(GridTerminalSystem);
            if (speek != null) speek.Play();

            if (Pb != null){ Pb.TryRun("Set:" + Enty.Position); return;}
            var ant = new Selection(null).FindBlock<IMyRadioAntenna>(GridTerminalSystem);
            if (ant == null) { Mess("Не найдены источники для передачи цели", true); return; }
            if (!ant.TransmitMessage("Kill:" + Enty.Position)) Mess("Отправка сообщения не удалась", true);
            else Mess("Отправка сообщения антеной -" + ant.CustomName, true);
        }
        catch (Exception e)
        {
            Echo(e.ToString());
            Me.CustomData += e.ToString();
        }
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
