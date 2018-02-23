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
    /* set,init:мотор горизонт, мотор верт(,текст. танель) - инициализация
        start/stop - запуск/остановка скрипта
        panels,solars:маска - маска имени солнечных панелей
        day:минуты - установка длительности дня
        ? - информация
        +,-(:сек) - Коректировка текущего угла
        sunpath(:угол) - вывод точек координат солнца
        `:номер строки - выполнить команду в строке CustomData
        sunaxis:вектор - установка вектора оси солнца
        sunaxis:вектор,вектор2 - расчет вектора оси солнца по двум векторам направления 
             (вектора можно поллучить скриптом info bloc, применив к любому блоку направленному на солнце, в два различных периода времени)
        angelx:угол - коректировка вектора земли, при неправельной устоановке ротора
        invz - Инвертирует поворот по вертикали

       --------------------------------------------------------------------------     
     AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: MyAndrey_ka@mail.ru      
     When using and disseminating information about the authorship is obligatory     
     При использовании и распространении информация об авторстве обязательна     
     ----------------------------------------------------------------------*/
    Program()
    {
        if (!Storage.StartsWith("SunRound_2") || Me.TerminalRunArgument == "null") return;
        try
        {
            var ids = Storage.Split('\n');
            var cou = ids.Length;
            int i = 2;
            TecSec = int.Parse(ids[i++]);
            MinInDay = int.Parse(ids[i++]);

            string s;
            if (!string.IsNullOrWhiteSpace(ids[i]))
            {
                MotorX = GridTerminalSystem.GetBlockWithId(long.Parse(ids[i++])) as IMyMotorStator;
                MotorY = GridTerminalSystem.GetBlockWithId(long.Parse(ids[i++])) as IMyMotorStator;
                s = ids[i++];
                if (!string.IsNullOrEmpty(s))
                    Panel = GridTerminalSystem.GetBlockWithId(long.Parse(s)) as IMyTextPanel;
                Data = new SunRound(ids, i);
                i = 13;
            }
            else i = 5;
            for (; i < cou; i++)
                if (!string.IsNullOrEmpty(ids[i]))
                {
                    var sp = GridTerminalSystem.GetBlockWithId(long.Parse(ids[i])) as IMySolarPanel;
                    if (sp != null) SolarPanels.Add(sp);
                }
            if (bool.Parse(ids[1])) Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Echo("Состояние востановлено");
        }
        catch (Exception e) { Echo("Error"); Me.CustomData += "\n\n" + e + "\n" + Storage; }

    }

    void Save()
    {
        StringBuilder res = new StringBuilder();
        res.AppendLine((Runtime.UpdateFrequency != UpdateFrequency.None).ToString());
        res.AppendLine(TecSec.ToString());
        res.AppendLine(MinInDay.ToString());
        if (MotorX != null)
        {
            res.AppendLine(MotorX.EntityId.ToString());
            res.AppendLine(MotorY.EntityId.ToString());
            res.AppendLine(Panel != null ? Panel.EntityId.ToString() : "");
            res.AppendLine(Data.Gr_Aix.ToString());
            res.AppendLine(Data.Gr_Zero.ToString());
            res.AppendLine(Data.Sun_Aix.ToString());
            res.AppendLine(Data.Sun_Zero.ToString());
            res.AppendLine(Data.InvZ.ToString());
        }
        else res.AppendLine(" ");
        SolarPanels.ForEach(x => res.AppendLine(x.EntityId.ToString()));
        Storage = "SunRound_2\n" + res.ToString();
    }

    static SunRound Data = new SunRound();
    int TecMS = 0, TecSec = 0, MinInDay = 120, Del;

    static IMyMotorStator MotorX, MotorY = null;
    static IMyTextPanel Panel = null;
    readonly List<IMySolarPanel> SolarPanels = new List<IMySolarPanel>();

    void Main(string argument, UpdateType Type)
    {
        TecMS += (int)Runtime.TimeSinceLastRun.TotalMilliseconds;

        if (Type <= UpdateType.Trigger)
        {
            if (!string.IsNullOrWhiteSpace(argument))
            {
                if (Panel != null) Panel.WritePublicText("");
                SetAtributes(argument.Split(';'));
            }
            return;
        }

        if (Del > 0) Del -= TecMS;
        TecSec += TecMS / 960;
        TecMS = TecMS % 960;

        if (TecSec > MinInDay * 60) TecSec = 0;
        if (Panel != null) Panel.WritePublicText("");

        float X, Z;
        var Tgr = MathHelper.TwoPi / MinInDay * TecSec / 60;
        var vSun = Data.GetToSun(Tgr);
        Data.GetAngels(vSun, out X, out Z);
        double Vel = X - MotorX.Angle;

        if (double.IsNaN(Vel)) Vel = 0;
        if (Vel < -Math.PI) Vel += MathHelper.TwoPi;
        else if (Vel > Math.PI) Vel -= MathHelper.TwoPi;

        MotorX.SetValue("Velocity", (Single)Vel * 4);

        Vel = Z - MotorY.Angle;
        if (double.IsNaN(Vel)) Vel = 0;
        if (Vel < -Math.PI) Vel += MathHelper.TwoPi;
        else if (Vel > Math.PI) Vel -= MathHelper.TwoPi;

        MotorY.SetValue("Velocity", (Single)Vel * 4);

        if (Panel != null && Del < 1)
        {
            StringBuilder s = new StringBuilder("Тек: " + TecSec / 60 + "м." + TecSec % 60 + "с. из " + MinInDay + "м.\n");
            s.AppendFormat("{0:0}° = {1:0.00000} рад.\n", MathHelper.ToDegrees(Tgr), Tgr);
            s.AppendFormat("A {0:0}°  Z {1:0}°\n", MathHelper.ToDegrees(X), MathHelper.ToDegrees(Z));
            s.AppendFormat("Тек: X {0:0.0} Z {1:0.0}\n", MathHelper.ToDegrees(MotorX.Angle), MathHelper.ToDegrees(MotorY.Angle));

            if (SolarPanels.Count > 0)
            {
                float Pow = 0, Mpow = 0;
                SolarPanels.ForEach(x => { Pow += x.CurrentOutput; Mpow += x.MaxOutput; });
                s.AppendFormat("Мощность: {0:0.##} из {1:0.##}мВт\nМощность: {2:0.##} из {3:0.##}мВт\n",
                    Pow, SolarPanels[0].CurrentOutput * 1000, Mpow, SolarPanels[0].MaxOutput * 1000);
            }
            TextOut(s.ToString(), false, false);
        }

    }

    void SetAtributes(string[] Args)
    {
        int Len = Args.GetLength(0);
        Delay(30);
        for (int i = 0; i < Len; i++)
        {
            string Arg = Args[i];
            if (Arg.Length == 0) continue;
            int pos = Arg.IndexOf(':');
            string Right;

            if (pos < 0) Right = "";
            else
            {
                Right = Arg.Substring(pos + 1);
                Arg = Arg.Remove(pos);
            }
            switch (Arg.ToLower())
            {
                case "`":
                    {
                        if (string.IsNullOrEmpty(Right))
                        { SetAtributes(Me.CustomData.Split('\n')); break; }
                        int g;
                        if (!int.TryParse(Right, out g))
                        { TextOut("Не верное значение строки \"" + Right + "\""); continue; }
                        var ms = Me.CustomData.Split('\n');
                        if (ms.GetLength(0) <= g)
                        { TextOut("Строка " + g + " не существует"); continue; }
                        SetAtributes(ms[g].Split(';'));
                    }
                    break;
                case "sunaxis":
                    {
                        var ss = Right.Split(',');
                        Vector3D tmp;
                        if (MyGPS.TryParseVers(ss[0], out tmp))
                        {
                            Data.Sun_Aix = Vector3D.Normalize(tmp);
                            if (ss.GetLength(0) > 1)
                                if (Vector3D.TryParse(ss[1], out tmp))
                                    Data.Sun_Aix = Vector3D.Normalize(Data.Sun_Aix.Cross(Vector3D.Normalize(tmp)));
                            TextOut("Установлена: " + V3DToStr(tmp, "Ось солнца", 50000));
                        }
                        else TextOut("Ошиба значения вектора");
                    }
                    break;
                case "start":
                    {
                        if (MotorX == null)
                        { TextOut("Сначала необходимо указать названия блоков. Используйте команду \"set\""); continue; }
                        if (SolarPanels.Count == 0) SetAtributes(new string[] { "panels" });
                        if (Len == 1) Delay(0);
                        Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    }
                    break;
                case "stop":
                    {
                        if (MotorX == null) continue;
                        MotorX.SetValue("Velocity", (Single)0);
                        MotorY.SetValue("Velocity", (Single)0);
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        TextOut("Выполнение остановлено");
                    }
                    break;
                case "angelx":
                    {
                        if (MotorX == null) continue;
                        float delta;
                        if (!float.TryParse(Right, out delta)) { TextOut("Ошиба значения угла"); continue; }
                        Data.Gr_Zero = SunRound.TurnVector(Data.Gr_Zero, Data.Gr_Aix, MathHelper.ToRadians(delta));
                        TextOut("Корректировка угла " + delta + "\n" + V3DToStr(Data.Gr_Zero, "Нулевой вектор земли", 10));
                    }
                    break;
                case "invz": Echo("Инверсия " + ((Data.InvZ = !Data.InvZ) ? "вкл" : "выкл")); break;
                case "?":
                    {
                        if (MotorX == null) continue;
                        StringBuilder s = new StringBuilder();
                        s.AppendLine("Длительность дня (мин.): " + MinInDay);
                        s.AppendLine(V3DToStr(Data.Gr_Aix, "Ось земли", 10));
                        s.AppendLine(V3DToStr(Data.Gr_Zero, "Нулевой вектор \"земли\"", 10));
                        s.AppendLine(V3DToStr(Data.Sun_Aix, "Ось солнца", 50000));
                        s.AppendLine(V3DToStr(Data.Sun_Zero, "Нулевой вектор солнца", 50000));
                        s.AppendLine(MotorX.WorldMatrix.ToString());
                        s.AppendLine(MotorY.WorldMatrix.ToString());

                        if (Data.InvZ) s.Append("Z");
                        TextOut(s.ToString(), false, true, true);
                    }
                    break;
                case "day":
                    TextOut(int.TryParse(Right, out MinInDay) ? "Установлена длительность дня " + MinInDay :
                        "Ошиба значения длительности дня");
                    break;
                case "+":
                    {
                        if (string.IsNullOrEmpty(Right)) pos = 60;
                        else if (!int.TryParse(Right, out pos))
                        { TextOut("Ошиба значения корректировки тек. времени"); continue; }
                        TecSec += pos;
                        TextOut(string.Format("Увеличено текущее время на {0} сек.\n{1}сек. ({2:0.#}°)",
                        pos, TecSec, 180f / MinInDay * 2 * TecSec / 60));
                    }
                    break;
                case "-":
                    {
                        if (string.IsNullOrEmpty(Right)) pos = 60;
                        else if (!int.TryParse(Right, out pos))
                        { TextOut("Ошиба значения корректировки тек. времени"); continue; }
                        TecSec -= pos;
                        while (TecSec < 0) TecSec += MinInDay * 60;
                        TextOut(string.Format("Уменьшено текущее время на {0} сек.\n{1}сек. ({2:0.#}°)",
                        pos, TecSec, 180f / MinInDay * 2 * TecSec / 60));
                    }
                    break;
                case "=":
                    {
                        if (string.IsNullOrEmpty(Right) || !int.TryParse(Right, out pos))
                        { TextOut("Ошиба значения тек. времени"); continue; }
                        TextOut(string.Format("Текущее время установлено {0} сек.", TecSec = (int)(MathHelper.TwoPi / MinInDay / 60 * pos)));
                    }
                    break;
                case "panels":
                case "solars":
                    {
                        new Selection(Right).FindBlocks<IMySolarPanel>(GridTerminalSystem, SolarPanels);
                        var s = new StringBuilder();
                        s.AppendLine("Найдено панелей - " + SolarPanels.Count + " :");
                        SolarPanels.ForEach(x => s.AppendLine(x.CustomName));
                        TextOut(s.ToString());
                    }
                    break;
                case "panel":
                    {
                        Panel = new Selection(Right).FindBlock<IMyTextPanel>(GridTerminalSystem);
                        if (Panel == null) { Echo("Не найдена панель - " + Right); continue; }
                        TextOut("Панель установлена");
                    }
                    break;
                case "set":
                case "init":
                    {
                        var names = Right.Split(',');
                        pos = names.GetLength(0);
                        if (pos < 2 || pos > 3)
                        {
                            TextOut("При установке требуется указать через запятую названия следующих блоков:\n" +
                                "Мотор гор., мотор верт. Также может быть добавлено название текстовой панели");
                            continue;
                        }
                        if (pos == 3) SetAtributes(new string[] { "panel:" + names[2] });
                        MotorY = GridTerminalSystem.GetBlockWithName(names[1]) as IMyMotorStator;
                        if (MotorY == null) { TextOut("Не найден вертикальный мотор - " + names[1]); continue; }
                        MotorX = GridTerminalSystem.GetBlockWithName(names[0]) as IMyMotorStator;
                        if (MotorX == null) { TextOut("Не найден горизонтальный мотор - " + names[0]); continue; }
                        Data.Gr_Aix = MotorX.WorldMatrix.Up;
                        Data.Gr_Zero = MotorX.WorldMatrix.Forward;
                        if (Vector3D.IsZero(Data.Sun_Aix))
                            Data.Sun_Aix = new Vector3D(0, 1, 0); // Для планет 
                        TextOut("Инициализация завершена успешно");
                        TecMS = 0;
                    }
                    break;
                case "sunpath":
                    {
                        var ps = Me.GetPosition();
                        if (Vector3D.IsZero(Data.Sun_Aix)) Data.Sun_Aix = new Vector3D(0, 1, 0);// Для планет
                        int shag;
                        if (Right.Length == 0 || !int.TryParse(Right, out shag)) shag = 30;
                        var s = new StringBuilder();
                        for (int a = 0; a < 360; a += shag)
                        {
                            var vSun = Data.GetToSun(MathHelper.ToRadians(a));
                            s.AppendLine(MyGPS.GPS("SP " + a + "°", ps + vSun * 100000));
                        }
                        TextOut(s.ToString(), true, false, true);
                    }
                    break;
                default:
                    Echo("Неизвестная команда: \"" + Arg + "\"" + Right);
                    break;
            }
        }
    }

    void TextOut(string Text, bool ToBar = true, bool append = true, bool ToCustomData = false)
    {
        if (Panel != null)
            if (ToCustomData) Panel.CustomData = Text;
            else Panel.WritePublicText(Text, append);
        if (ToBar || Panel == null) Echo(Text);
    }
    string V3DToStr(Vector3D val, string name, int dist)
    { return (MotorX == null) ? name + ": " + val : MyGPS.GPS(name, val, MotorX.GetPosition(), dist); }
    void Delay(int ms = 120) { if (Del < ms + TecMS) Del = ms + TecMS; }


    public class SunRound
    {
        public bool InvZ = false;
        public Vector3D Gr_Aix, Gr_Zero;
        Vector3D Sun_Aix_, Sun_Zero_;

        public SunRound() { }
        public SunRound(string[] ids, int beg)
        {
            Vector3D.TryParse(ids[beg], out Gr_Aix);
            Vector3D.TryParse(ids[beg + 1], out Gr_Zero);
            Vector3D.TryParse(ids[beg + 2], out Sun_Aix_);
            Vector3D.TryParse(ids[beg + 3], out Sun_Zero_);
            bool.Parse(ids[beg + 4]);
        }

        public Vector3D Sun_Zero { get { return Sun_Zero_; } }
        public Vector3D Sun_Aix
        {
            get { return Sun_Aix_; }
            set
            {
                Sun_Aix_ = value; Sun_Zero_ = Vector3D.CalculatePerpendicularVector(Sun_Aix_);
            }
        }

        public static Vector3D TurnVector(Vector3D val, Vector3D Axis, float angel)
        {
            var M = Matrix.CreateFromAxisAngle(Axis, angel);
            return Vector3D.Normalize(Vector3D.Transform(val, M));
        }
        public Vector3D GetToSun(float angel)
        {
            if (Vector3D.IsZero(Sun_Zero_))
                Sun_Zero_ = Vector3D.CalculatePerpendicularVector(Sun_Aix_);
            return TurnVector(Sun_Zero_, Sun_Aix_, angel);
        }
        public void GetAngels(Vector3D SunPos, out float X, out float Z)
        {
            var PrToGr = Vector3D.Reject(SunPos, Gr_Aix);
            X = MyMath.AngleBetween(Gr_Zero, PrToGr);
            Z = MyMath.AngleBetween(SunPos, PrToGr);
            if (Vector3D.Dot(Gr_Zero.Cross(Gr_Aix), PrToGr) < 0) X = MathHelper.TwoPi - X;
            if (Vector3D.Dot(Sun_Aix_, PrToGr) < 0) Z = MathHelper.TwoPi - Z;
            if (InvZ) Z = -Z;
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


}
