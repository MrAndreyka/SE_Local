using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;





namespace ConsoleApplication1
{


    class Program
    {
        static System.Globalization.CultureInfo SYS = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU");
        public class IMyTextPanel
        {
            //Sandbox.ModAPI.Ingame.IMyTextPanel
            public string CustomName;
            public Vector3I Min, Max;
            public IMyTextPanel(string val) { CustomName = val;
                Min = new Vector3I(1, 1, 1); Max = new Vector3I(1, 2, 1); }
            public double FontSize = 1.25;
            public string EntityId { get => CustomName; }
            public void WritePublicText(string val, bool next = true) { Console.Write(CustomName + "-> " +val + (next ? "\r\n" : "")); }
            public static explicit operator string(IMyTextPanel v) { return v.CustomName; }
            public static implicit operator IMyTextPanel(string v) { return new IMyTextPanel(v); }
         }

        class Translate : Dictionary<String, string>
        {

            public delegate bool myPredicate(string myInt);
            public Translate()
            {
                var a = ("Components,*компоненты*,PowerCell,Аккумулятор,SmallTube,Малая труба,Girder,Балка,SteelPlate,Стальная плаcтина,LargeTube,Большая труба," +
               "MetalGrid,Металлическая решетка,SolarCell,Солнечная батарея,BulletproofGlass,Бронированное стекло,Motor,Мотор,Computer,Компьютер,Display,Экран," +
               "Reactor,Реактор,Construction,Строительный компонент,Detector,Компоненты детектора,GravityGenerator,Компоненты гравитационного генератора," +
               "InteriorPlate,Пластина,Medical,Медицинские компоненты,SmallSteelTube,Маленькая стальная трубка,Thrust,Детали ускорителя," +
               "RadioCommunication,Комплектующие для радио-связи,Ingot,*слитки*,CobaltIngot,Кобальтовый слиток,GoldIngot,Золотой слиток,StoneIngot,Гравий," +
               "IronIngot,Железный слиток,MagnesiumIngot,Порошок магния,NickelIngot,Никелевый слиток,PlatinumIngot,Платиновый слиток,SiliconIngot,Кремниевая пластина," +
               "SilverIngot,Серебряный слиток,UraniumIngot,Слиток урана,Ore,*руда*,CobaltOre,Кобальтовая руда,PlatinumOre,Платиновая руда,NickelOre,Никелевая руда," +
               "GoldOre,Золотая руда,StoneOre,Камень,IceOre,Лёд,IronOre,Железная руда,ScrapOre,Металлолом,MagnesiumOre,Магниевая руда,Potassium,Калий," +
               "SiliconOre,Кремниевая руда,SilverOre,Серебряная руда,UraniumOre,Урановая руда,Ammo,*боеприпасы*,NATO_25x184mm,Контейнер боеприпасов 25x184 мм НАТО," +
               "NATO_5p56x45mm,Магазин 5.56x45мм НАТО,Missile200mm,Контейнер 200мм ракет,Explosives,Взрывчатка,HandTool,*инструменты*," +
               "OxygenBottle,Кислородный баллон,HydrogenBottle,Водородный баллон,Welder,Сварочный аппарат,Welder2,Сварочный аппарат II," +
               "Welder3,Сварочный аппарат III,Welder4,Сварочный аппарат IV,AngleGrinder,Дробилка,AngleGrinder2,Дробилка II,AngleGrinder3,Дробилка III," +
               "AngleGrinder4,Дробилка IV,HandDrill,Отбойный молоток,HandDrill2,Отбойный молоток II,HandDrill3,Отбойный молоток III,HandDrill4,Отбойный молоток IV," +
               "AutomaticRifle,Автоматическая винтовка,PreciseAutomaticRifle,Автоматическая винтовка II,RapidFireAutomaticRifle,Автоматическая винтовка III," +
               "UltimateAutomaticRifle,Автоматическая винтовка IV").Split(',');
                var sz = a.Length - 1;
                for (var i = 0; i < sz; i += 2) Add(a[i], a[i + 1]);
            }

            public KeyValuePair<String, string> Find(myPredicate Pred)
            {
                foreach (var recordOfDictionary in this) if (Pred(recordOfDictionary.Value) || Pred(recordOfDictionary.Key)) return recordOfDictionary;
                return new KeyValuePair<String, string>();
            }

            public string GetParentByKey(myPredicate Pred)
            {
                var res = string.Empty;
                foreach (var recordOfDictionary in this)
                {
                    if (recordOfDictionary.Value.StartsWith("*")) res = recordOfDictionary.Key;
                    if (Pred(recordOfDictionary.Key)) return res;
                }
                return string.Empty;
            }

            public string GetName(string clas) { string res = string.Empty; if (!TryGetValue(clas, out res)) res = clas; return res; }
        }
        static Translate LangDic = new Translate();

        public delegate string GetNames(string val);
       
        public class FinderItem
        {
            public readonly byte Type; public readonly string Name;
            public FinderItem(byte _Type, string _Name) { Type = _Type; Name = _Name; }
            public bool IsSuitable(FinderItem val) { return Type == 0 ? val.Name == Name : val.Type == Type; }
            public bool Equals(FinderItem obj) => Name.Equals(obj.Name);
            public override int GetHashCode() => Name.GetHashCode();
            public override string ToString() { return string.Format("{0}:{1}", Type, Name); }
            public string ToString(GetNames Ut) { return Ut(Name); }
            public static bool TryParse(string val, out FinderItem res)
            {
                var ta = val.Split(':'); byte t;
                if (ta.Length != 2 || !byte.TryParse(ta[0], out t)) { res = null; return false; }
                res = new FinderItem(t, ta[1]);
                return true;
            }
            public static FinderItem Parse(string val)
            { FinderItem res; if (TryParse(val, out res)) throw new Exception("Error in parse"); return res; }
        }
        public class FindItem : FinderItem
        {
            public bool inv;
            public FindItem(byte _Type, string _Name, bool _inv = false) : base(_Type, _Name) { inv = _inv; }
            public bool Equals(FindItem obj) { return Type == obj.Type && Name == obj.Name && inv == obj.inv; }
            public override string ToString() { return string.Format("{0}:{1}:{2}", Type, Name, inv); }
            public new string ToString(GetNames Ut) { return (inv ? "!" : "") + Ut(Name); }
            public new static FindItem Parse(string val)
            {
                var ta = val.Split(':');
                if (ta.Length != 3) return null;
                return new FindItem(byte.Parse(ta[0]), ta[1], bool.Parse(ta[2]));
            }
        }
        public class FindList : List<FindItem>
        {
            public FindList() { }
            public FindList(IEnumerable<FindItem> collection) : base(collection) { }
            public bool Equals(FindList val) => Count == val.Count && Count > 0 && FindIndex(x => val.FindIndex(y => x.Equals(y)) < 0) < 0;
            public new void Add(FindItem val) { if (FindIndex(x => x.Equals(val)) < 0) { base.Add(val);
                    Sort(delegate(FindItem a, FindItem b) {return a.inv==b.inv?0:(a.inv?-1:1); }); } }
            public void AddNonExisting(FindList val) => AddRange(val.FindAll(x => FindIndex(y => y.Equals(x)) >= 0));
            public bool IsInclude(FinderItem val)
            {
                bool all_inv = true;
                for (var i = 0; i < Count; i++)
                {
                    if (all_inv && !this[i].inv) all_inv = false;
                    if (this[i].IsSuitable(val)) return !this[i].inv;
                }
                return all_inv;
            }
            public override string ToString() => string.Join(",", this);
            public string ToString(GetNames Ut)
            {
                var t = new List<string>();
                ForEach(x => t.Add(x.ToString(Ut)));
                return string.Join(",", t);
            }
            public static FindList Parse(string val)
            {
                var sr = val.Split(',');
                FindList res = new FindList();
                if (!string.IsNullOrEmpty(val)) for (var i = 0; i < sr.Length; i++) res.Add(FindItem.Parse(sr[i]));
                return res;
            }
        }


        public class Abs_Plane
        {
            public delegate bool AsPanel(IMyTextPanel x);
            public delegate TypeOfData GetVal<TypeOfData>(string x);
            public object Owner = null;

            protected Abs_Plane() { }
            public Abs_Plane GetThis(object Own) { Owner = Own; return this; }
            public virtual string ToSave() => string.Empty;
            public virtual bool ShowText(string[] vals) => false;
            public virtual void ToBegin() { }
            public virtual TextPanel Find(AsPanel val) => null;
            public static Abs_Plane Parse(GetVal<IMyTextPanel> Gr, string val)
            { Abs_Plane Par; if (TryParse(Gr, val, out Par)>=0) throw new Exception(""); return Par; }
            public static int TryParse(GetVal<IMyTextPanel> Gr, string val, out Abs_Plane res)
            {
                res = null;
                Abs_Plane tmp = null;
                int b = -1, co = 0;
                char ch;
                string s;
                for (var i = 0; i < val.Length; i++)
                {
                    ch = val[i];
                    if (ch == '{') { if (co++ == 0) b = i; continue; }
                    if (ch == '}')
                    {
                        if (co-- == 0) return i;
                        else if (co == 0)
                        {
                            s = val.Substring(b + 1, i - b - 1);
                            var code = TryParse(Gr, s, out tmp);
                            if (code>-10) return b+1+code;
                            if (res == null)
                                if (b > 0)
                                {
                                    byte tpb; if (!byte.TryParse(val.Substring(0, b - 1), out tpb)) return 0;
                                    if (tpb < 2) res = new TextPlane(tpb == 1, tmp);
                                    else if (tmp is TextPanel) res = new LinePlane(tmp as TextPanel);
                                    else return b+1;
                                }
                                else res = tmp;
                            else if (!(res is TextPlane)) return b + 1;
                            else (res as TextPlane).Add(tmp);
                            b = ++i;
                        }
                        continue;
                    }
                    if (co > 0) continue;
                    if (ch == ';')
                    {
                        s = val.Substring(b + 1, i - b - 1);
                        
                        if (s.Length == 0 && val[b] != '}') return b + 1;
                        int code = TryOneParse(Gr, s, out tmp);
                        if (code>-10) return code+b+1;
                        if (res == null) res = tmp;
                        else if (!(res is TextPlane)) return b + 1;
                        else (res as TextPlane).Add(tmp);
                        b = i;
                    }
                }
                if (co > 0) return val.Length - 1;
                co = val.Length - 1;
                if (b < co)
                {
                    var code = TryOneParse(Gr, val.Substring(b + 1, co - b), out tmp);
                    if (code>-10) return code+b+1;
                    if (res == null) res = tmp;
                    else if (!(res is TextPlane)) return b+1;
                    else (res as TextPlane).Add(tmp);
                }
                return -10;
            }
            private static int TryOneParse(GetVal<IMyTextPanel> Gr, string val, out Abs_Plane res)
            {
                var arp = val.Split('>');
                res = null;
                switch (arp.Length)
                {
                    case 1: { var tp = Gr(arp[0]); if (tp == null) return -1; res = new TextPanel(tp); } break;
                    case 2:
                        {
                            byte b; Abs_Plane tmp = null;
                            if (!byte.TryParse(arp[0], out b)) return 0;
                            var code = TryOneParse(Gr, arp[1], out tmp);
                            if(code>-10) return code + arp[0].Length;
                            if (b < 2) res = new TextPlane(b == 1, tmp);
                            else if (tmp is TextPanel) res = new LinePlane(tmp as TextPanel);
                            else return code + arp[0].Length;
                        }
                        break;
                    default: return 0;
                }
                return -10;
            }
        }

        public class TextPanel : Abs_Plane
        {
            public readonly IMyTextPanel panel; int _cou = 0;
            public TextPanel(IMyTextPanel pan) { panel = pan; }
            public override string ToString() => panel.CustomName;
            public override string ToSave() => panel.EntityId.ToString();
            public override bool ShowText(string[] vals) => ShowText(GetStr(vals));
            public bool ShowText(string val)
            { panel.WritePublicText(val + "\n", true); return ++_cou >= (int)(18 / panel.FontSize); }
            public override void ToBegin() { _cou = 0; panel.WritePublicText(String.Empty); }
            public override TextPanel Find(AsPanel val) => val(panel) ? this : null;
            public int GetLines() => 0;
            public static string GetStr(string[] vals)
            {
                switch (vals.Length)
                {
                    case 0: return string.Empty;
                    case 1: return vals[0];
                    default: return string.Format(vals[0], vals);
                }
            }
        }

        public class TextPlane : Abs_Plane
        {
            public List<Abs_Plane> Planes { get; } = new List<Abs_Plane>();
            public bool Hor;
            int _Tec = 0;
            public void Add(Abs_Plane val) => Planes.Add(val.GetThis(this));
            public override void ToBegin() { _Tec = 0; Planes.ForEach(x => x.ToBegin()); }
            public TextPlane(bool horis, Abs_Plane First, params Abs_Plane[] other)
            { Hor = horis; Planes.Add(First.GetThis(this)); for (var i = 0; i < other.Length; i++) Planes.Add(other[i].GetThis(this)); }
            public override string ToString() => string.Format("{{{0}:{1}}}", Hor ? "Horizontal" : "Vertical", string.Join(", ", Planes));
            public override string ToSave()
            {
                var res = new StringBuilder("{" + (Hor ? 1 : 0) + ">");
                var tmp = new List<string>(Planes.Count);
                Planes.ForEach(x => tmp.Add(x.ToSave()));
                res.Append(string.Join(";", tmp) + "}");
                return res.ToString();
            }
            public override bool ShowText(string[] text)
            {
                var Max = Planes[_Tec].ShowText(text);
                if (Planes.Count == 1) return Max;
                if (Hor) { if (++_Tec == Planes.Count) _Tec = 0; else Max = false; }
                else if (Max && Planes.Count - _Tec > 1) _Tec++;
                return Max;
            }
            public override TextPanel Find(AsPanel val)
            {
                TextPanel res = null;
                for (var i = 0; i < Planes.Count && res == null; i++) res = Planes[i].Find(x => val(x));
                return res;
            }
        }

        public class LinePlane : TextPlane
        {
            public void Add(TextPanel val) => base.Add(val);
            public LinePlane(TextPanel First, params TextPanel[] other) : base(false, First, other) { }
            public override string ToString() => $"{{InLine:{string.Join(", ", Planes)}}}";
            public override string ToSave()
            {
                var res = new StringBuilder("{" + 2 + ">");
                var tmp = new List<string>(Planes.Count);
                Planes.ForEach(x => tmp.Add(x.ToSave()));
                res.Append(string.Join(";", tmp) + "}");
                return res.ToString();
            }
            public override bool ShowText(string[] text)
            {
                int ml = text.Length - 1, _mx = Math.Min(Planes.Count - 1, ml), i;
                if (ml < 1) return (Planes[0] as TextPanel).ShowText(text);
                var MAX = false;
                for (i = 0; i < _mx; i++) MAX = (Planes[i] as TextPanel).ShowText(text[i + 1]) || MAX;
                _mx = ml - Planes.Count;
                if (_mx == 0) MAX = (Planes[i] as TextPanel).ShowText(text[i + 1]) || MAX;
                else if (_mx > 0)
                {
                    ml = text[0].IndexOf("{" + (i + 1) + "}");
                    MAX = (Planes[i] as TextPanel).ShowText(string.Format(text[0].Substring(ml), text)) || MAX;
                }
                return MAX;
            }
        }

        public class FindPlane : FindList
        {
            public List<string[]> Texts { get; } = new List<string[]>();
            public readonly Abs_Plane Plane;

            public FindPlane(Abs_Plane Tp) { Plane = Tp.GetThis(this); }
            public FindPlane(FindList Fl, Abs_Plane Tp) : base(Fl) { Plane = Tp.GetThis(this); }
            public void ShowText() => Texts.ForEach(x => Plane.ShowText(x));
            public override string ToString() => base.ToString(LangDic.GetName) + "-" + Plane.ToString();
            public string ToSave() => base.ToString() + "-" + Plane.ToSave();
        }

        public class TextsOut : List<FindPlane>
        {
            public StringBuilder Uncown { get; } = new StringBuilder();

            public bool AddText(FinderItem Type, params string[] Val)
            {
                var res = Find(x => x.IsInclude(Type));
                if (res == null) { Uncown.AppendLine(TextPanel.GetStr(Val)); return false; }
                res.Texts.Add(Val);
                return true;
            }
            public bool AddText(MyInvItem Sel) { return AddText(Sel.Lnk, "{1}: {2}", Sel.ShowName, Sel.count.ToString("#,##0.##", SYS)); }
            public bool AddText(params string[] Val)
            {
                var res = Find(x => x.Count == 0);
                if (res == null) { Uncown.AppendLine(TextPanel.GetStr(Val)); return false; }
                res.Texts.Add(Val);
                return true;
            }
            public void ShowText() => ForEach(x => x.ShowText());
            public bool FindPanel(Abs_Plane.AsPanel val, out FindPlane Fp, out TextPanel Tp)
            {
                TextPanel Tp_ = null;
                Fp = Find(fp => { Tp_ = fp.Plane.Find(x => val(x)); return Tp_ != null; });
                return (Tp = Tp_) != null;
            }
            public void ClearText() { ForEach(x => { x.Texts.Clear(); x.Plane.ToBegin(); }); Uncown.Clear(); }
        }

        public class MyInvIt : FinderItem
        {
            public string ShowName { get; protected set; }
            static Dictionary<VRage.ObjectBuilders.MyObjectBuilder_Base, MyInvIt> Names = new Dictionary<VRage.ObjectBuilders.MyObjectBuilder_Base, MyInvIt>();

            public MyInvIt(string Name, bool SetShow, byte Type = 0) : base(Type, Name) { if (SetShow) SetShowName(); }
            public static MyInvIt Get(VRage.ObjectBuilders.MyObjectBuilder_Base val)
            {
                MyInvIt v;
                if (Names.TryGetValue(val, out v)) return v;

                string s = val.TypeId.ToString(), name = val.SubtypeName;

                if (s.EndsWith("Ore")) name += "Ore";
                else if (s.EndsWith("Ingot")) name += "Ingot";
                else if (s.EndsWith("GunObject")) name = name.Replace("Item", "");
                s = s.Substring(16);
                v = new MyInvIt(name, true, GetType(s));
                Names.Add(val, v);
                return v;
            }

            public static byte GetType(string val)
            {
                byte tp;
                switch (val)
                {
                    case "Component": tp = 2; break;
                    case "PhysicalGunObject": tp = 3; break;
                    case "AmmoMagazine": tp = 4; break;
                    case "Ore": tp = 5; break;
                    case "Ingot": tp = 6; break;
                    case "Components": tp = 2; break;
                    case "HandTool": tp = 3; break;
                    case "Ammo": tp = 4; break;
                    default: tp = 7; break;
                }
                return tp;
            }
            void SetShowName() => ShowName = LangDic.GetName(Name);
            public override string ToString() => ShowName;
        }
        public class MyInvItem
        {
            public readonly MyInvIt Lnk;
            public double count;
            public string ShowName { get { return Lnk.ShowName; } }

            public MyInvItem(MyInvIt val, double Cou = 0) { Lnk = val; count = Cou; }
            public MyInvItem(IMyInventoryItem val)
            {
                Lnk = MyInvIt.Get(val.Content);
                count = (double)val.Amount;
            }
            protected MyInvItem(string value) : this(new MyInvIt(value, true, MyInvIt.GetType(LangDic.GetParentByKey(x => x.Equals(value))))) { }

            public override string ToString() => Lnk.ShowName + ": " + count.ToString("### ##0.##");
            public string ToString(string msk) => string.Format(msk, Lnk.Name, Lnk.ShowName, count, Lnk.Type);
            public MyInvItem Clone(double Count = double.NaN) => new MyInvItem(Lnk) { count = Count == double.NaN ? this.count : Count };
            public static MyInvItem Parse(string val)
            { MyInvItem res; if (!TryParse(val, out res)) throw new Exception("Error in parse"); return res; }
            public static bool TryParse(string val, out MyInvItem res)
            {
                res = null;
                var ss = val.Split('|');
                byte tp; double cou;
                if (!byte.TryParse(ss[1], out tp) || !double.TryParse(ss[2], out cou)) return false;
                res = new MyInvItem(new MyInvIt(ss[0], true, tp)) { count = cou };
                return true;
            }
        }



        static void Main(string[] args)
        {
            Go2(args);
            Console.Write("....any key....");
            Console.ReadKey();
        }

        static void Go(string[] args)
        {
            double a = 0.12345678;
            for (int i = 0; i < 5; i++)
            {
                a *= 10;
                Console.WriteLine(System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU").ToString());
                Console.WriteLine(a.ToString("+#,###.##", System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("RU")));
                Console.WriteLine();

            }

        }

        static void Go2(string[] args)
        {

            var Out = new TextsOut();
            var set = new List<string>
            {
                "6:Ingot:False-{0>{2>ЖК верт панель; ЖК 1 верт панель};{2>ЖК верт панель 2; ЖК 1 верт панель 2};ЖК верт панель 3}",
                "5:Ore:false,0:AceOre:true-{1>*LCD Top;*LCD Top 2;*LCD Top 3}",
                "0:Камень:false-LCD Top 3"};

           /* /  {
                 "11::False-{2>1:11;1:12;1:13}",
                 "12::False-{0>2:11;{1>2:21;2:22}}",
                 "13::False-3:11",
                 "14::False-{0>{1>4:11;4:12};4:21}",
                 "15::False-{1>{0>5:11;5:21};5:12}",
                 "16::False-{1>{0>6:11;{1>6:21;{2>6:22;6:23}}};6:12}"

                    11::False-{2>1:11;1:12;1:13}
                    12::False-{0>2:11;{1>2:21;2:22}}
                    13::False-3:11
                    14::False-{1>4:11;4:12;;4:21}
                    15::False-{0>5:11;5:21;;5:12}
                    16::False-{0>6:11;{1>6:21;{2>6:22;6:23}};;6:12}
             };*/


              set.ForEach(x => {
                  var st = x.Split('-');
                  Abs_Plane tmp;
                  var res = Abs_Plane.TryParse(z => new IMyTextPanel(z), st[1], out tmp);
                  if (res > -10) Console.WriteLine(res);
                  else Out.Add(new FindPlane(FindList.Parse(st[0]), tmp));

              });
              //*/
            /*byte y = 10;
            var fl = new FindList() { new FindItem(++y, "") };
            Out.Add(new FindPlane(fl, new TextPlane(TextPlane.TypePlane.InLine, new TextPanel("1:11"), new TextPanel("1:12"), new TextPanel("1:13"))));
            fl = new FindList() { new FindItem(++y, "") };
            Out.Add(new FindPlane(fl, new TextPlane(TextPlane.TypePlane.Vert,   new TextPanel("2:11"), 
                    new TextPlane(TextPlane.TypePlane.Hor, new TextPanel("2:21"), new TextPanel("2:22")))));
            fl = new FindList() { new FindItem(++y, "") };
            Out.Add(new FindPlane(fl, new TextPanel("3:11")));
            fl = new FindList() { new FindItem(++y, "") };
            Out.Add(new FindPlane(fl, new TextPlane(TextPlane.TypePlane.Vert, new TextPlane(TextPlane.TypePlane.Hor,  new TextPanel("4:11"), new TextPanel("4:12")), 
                new TextPanel("4:21"))));
            fl = new FindList() { new FindItem(++y, "") };
            Out.Add(new FindPlane(fl, new TextPlane(TextPlane.TypePlane.Hor, new TextPlane(TextPlane.TypePlane.Vert, new TextPanel("5:11"), new TextPanel("5:21")),
                new TextPanel("5:12"))));
            fl = new FindList() { new FindItem(++y, "") };
            Out.Add(new FindPlane(fl, 
                new TextPlane(TextPlane.TypePlane.Hor, 
                        new TextPlane(TextPlane.TypePlane.Vert, new TextPanel("6:11"), 
                                new TextPlane(TextPlane.TypePlane.Hor, new TextPanel("6:21"), 
                                        new TextPlane(TextPlane.TypePlane.InLine, new TextPanel("6:22"), new TextPanel("6:23"))
                                        )
                                ),
                        new TextPanel("6:12")
                 )
            ));//*/


            Out.ForEach(x=>Console.WriteLine(x));
            Console.WriteLine();

            for (var j = 0; j < 100; j++)
                for (var i = 1; i < 7; i++)
                {
                    var t = new List<string>(6) { "{1}: {2}: {3}: {4}: {5}: {6}" };
                    for (var r = 0; r < 6; r++) t.Add(string.Format("{0}_{1}_{2}  ", i, j, r));
                    Out.AddText(new FinderItem((byte)i, ""), t.ToArray());
                }

            Out.AddText(new FinderItem((byte)5, "AceOre"), "Лед");
            Out.AddText(new FinderItem((byte)5, "IronOre"), "Железо");
            Out.AddText(new FinderItem((byte)5, "GoldOre"), "Золото");

            Out.ShowText();
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>> <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            Console.WriteLine(Out.Uncown);

            Console.WriteLine("\n>>>>>>>>>>>>>>>>>>>>>>>>>>> <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            Out.ForEach(x=>Console.WriteLine(x.ToSave()));
        }

    }
}
