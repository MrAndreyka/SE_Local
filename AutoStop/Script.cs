/*--------------------------------------------------------------------------     
 AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: MyAndrey_ka@mail.ru      
 When using and disseminating information about the authorship is obligatory     
 При использовании и распространении информация об авторстве обязательна     
 ----------------------------------------------------------------------*/
    static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");
    // CustomData  
    //  Коннектор  
    //  Таймер включения  
    //  Группа выключения 
    // ? Таймер проверки  

    readonly IMyShipConnector St;
    readonly Timer Tm;
    readonly IMyTimerBlock Go;
    static List<IMyFunctionalBlock> Lst;

    Program()
    {
        var a = Me.CustomData.Split('\n');
        var i = a.GetLength(0);
        if (i < 3)
        {Echo("Ошибка инициализации, не хватает параметров");return;}

        St = GridTerminalSystem.GetBlockWithName(a[0]) as IMyShipConnector;
        Go = GridTerminalSystem.GetBlockWithName(a[1]) as IMyTimerBlock;
        new Selection(a[2]).FindBlocks<IMyFunctionalBlock>(GridTerminalSystem, Lst);
        Tm = new Timer(this);
    }

    void Main(string s, UpdateType upt = UpdateType.Once)
    {
        if (upt < UpdateType.Update1)
        {
            if (s == "Stop") Lst.ForEach(x => x.Enabled = false);
            else if (s == "On") Tm.Interval = 960;
            else if (s == "Off") Tm.Stop();
            return;
        }
        else if (Tm.Run() == 0 || St == null) return;

        if (St.Status == MyShipConnectorStatus.Connectable) Go.Trigger();
        else if (St.Status == MyShipConnectorStatus.Connected) Tm.Stop();

        Echo(St.Status.ToString());
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
        public Timer(MyGridProgram Owner, int tc = 0, bool zer = false) { GP = Owner; TC = tc; zeroing = zer; }
        public int Interval
        {
            get { return Int; }
            set { SetInterval(RoundInt(value), zeroing); }
        }
        public void Stop() { GP.Runtime.UpdateFrequency = UpdateFrequency.None; Int = 0; }
        public static Point RoundInt(int value)
        {
            Point v;
            int del;
            if (value <= 960) { del = 16; v.Y = 1; }
            else if (value < 4000) { del = 160; v.Y = 2; }
            else { del = 1600; v.Y = 3; }
            v.X = value % del;
            if (v.X > del / 2) value += del;
            v.X = value - v.X;
            return v;
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
            return new Timer(gp, int.Parse(s[0]), bool.Parse(s[1])) { Interval = int.Parse(s[2]) };
        }
    }
