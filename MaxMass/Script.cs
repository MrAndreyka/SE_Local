 /*----------------------------------------------------------------------   
      AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com   
      When using and disseminating information about the authorship is obligatory   
      При использовании и распространении информация об авторстве обязательна   
      ----------------------------------------------------------------------*/
        Program()
        {
            var f = Me.CustomData.Split('\n');
            if (f.Length > 0) TP = new Selection(f[0]).FindBlock<IMyTextPanel>(GridTerminalSystem);
            RemCon = new Selection(f.Length > 1 ? f[1] : "").FindBlock<IMyShipController>(GridTerminalSystem);
            TM = new Timer(this);
            if (f.Length > 2) TM.Interval = int.Parse(f[2]);
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

        static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");
        IMyTextPanel TP;
        IMyShipController RemCon;
        Timer TM;
        static DirData AllThrusts = new DirData();
        //static VirtualThrusts Thrusts = new VirtualThrusts();
        bool shortV;
        static double Height;

        // ---------------------------------- MAIN --------------------------------------------- 
        void Main(string argument, UpdateType tpu)
        {
            if (RemCon == null) { Echo("На корабле нет блока управления"); return; }

            if (AllThrusts[0].Count == 0 || tpu < UpdateType.Update1 & !string.IsNullOrEmpty(argument))
            {
                var ars = argument.Split(':');
                shortV = ars.Length > 1;

                var TrL = new List<IMyThrust>();
                new Selection(ars[0]).FindBlocks<IMyThrust>(GridTerminalSystem, TrL);
                if (TrL.Count == 0) { Echo("Не найдены трастеры"); return; }

                AllThrusts.ForEach((x, y) => x.Clear());
                TrL.ForEach(x =>
                {
                    if (!x.IsFunctional)
                    { Echo(x.CustomName + " не функционирует"); return; }
                    else
                        AllThrusts[Base6Directions.GetClosestDirection(x.GridThrustDirection)].
                            Add(x, TrustsData.GetTypeFromSubtypeName(x.BlockDefinition.SubtypeName));
                    Echo(x.CustomName + "- " + Base6Directions.GetClosestDirection(x.GridThrustDirection));
                });
            }
            if (TM.Run() == 0) return;

            if (TP != null) TP.WritePublicText("");

            if (!RemCon.TryGetPlanetElevation(MyPlanetElevation.Surface, out Height)) Height = 0;
            var mas = RemCon.CalculateShipMass().TotalMass;
            var grav = RemCon.GetNaturalGravity().Length();

            var s = new StringBuilder(shortV ? $"M:{mas.ToString("###,##0", SYS)}" : $"Mass: {mas.ToString("###,##0", SYS)}кг");
            if (Height > 0)
                s.Append((shortV ? "В:" : " Высота: ") + Height.ToString("###,##0", SYS));

            s.AppendLine((shortV ? "Gr:" : " Грав: ") + grav.ToString("0.##"));

            foreach (Base6Directions.Direction y in Enum.GetValues(typeof(Base6Directions.Direction)))
            {
                var vals = AllThrusts[y].GetValues();
                s.AppendLine(y.ToString());
                if (Height > 0)
                    if (shortV) s.Append(string.Format(SYS, "Max:{0:###,##0}\n", vals.EffectivePow / grav));
                    else s.Append(string.Format(SYS, "Max:{0:###,##0}кг. ", vals.EffectivePow / grav));

                if (shortV)
                    s.AppendLine(string.Format(SYS, "Уск:{0:0.0#}_{2}={1}/{3}",
                    vals.EffectivePow / mas, vals.Max_pow, AllThrusts[y].Count, vals.EffectivePow));
                else
                    s.AppendLine(string.Format(SYS, "Ускор: {0:0.0#}м/c. Cou: {2} = {1}/{4}\nCoof:{3:0.00}",
                    vals.EffectivePow / mas, vals.Max_pow, AllThrusts[y].Count, vals.TecCoof, vals.EffectivePow));
            }

            Echo_(s.ToString());
        }
        // ---------------------------------- end MAIN ---------------------------------------------  

        void Echo_(string text, bool append = true) { if (TP == null) Echo(text); else TP.WritePublicText(text + "\n", append); }

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
            { GP = Owner;
                if (Inter == 0) zeroing = zer;
                else
                {
                    var P = new Point(Inter, 0);
                    CallFrequency(ref P);
                    SetInterval(P, zer);
                }
                TC = tc; }
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
