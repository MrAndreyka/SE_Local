using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using Sandbox.Game.Components;
using VRage.Game.Localization;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SpaceEngineers.Game.ModAPI.Ingame;

class Program : MyGridProgram
{
    /*--------------------------------------------------------------------------       
    AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: MyAndrey_ka@mail.ru        
    When using and disseminating information about the authorship is obligatory
    При использовании и распространении информация об авторстве обязательна       
    ----------------------------------------------------------------------*/

    Follower Follower1;
    static ShowMes txt = new ShowMes();
    Vector3D tecTarg;
    static bool Flag = false;
    static float MaxSpeed = 100;

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
            if (txt.Txt != null) txt.Txt.CustomData = "";
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }
        else if (Flag && UT == UpdateType.Update10)
        {
            txt.SetPoint();
            if (!Vector3D.IsZero(tecTarg)) Follower1.GoToPos(tecTarg);
            txt.Show(this);
        }
        else if (!string.IsNullOrWhiteSpace(arg)) { SetAtributes(arg); txt.Show(this); }
    }

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
                else Right = "";
                Arg = Arg.ToLower();
                switch (Arg)
                {
                    case "=":
                        MyGPS.TryParseVers(Right, out tecTarg);
                        Runtime.UpdateFrequency = UpdateFrequency.Update10;
                        Flag = true;
                        break;
                    case "~=":
                        MyGPS.TryParseVers(Right, out tecTarg);
                        Runtime.UpdateFrequency = UpdateFrequency.None;
                        break;
                    case "+":
                        {
                            int a;
                            if (!int.TryParse(Right, out a)) { Echo("Ошибка чтения дистанции"); return; }
                            tecTarg = Follower.RemCon.GetPosition() + Follower.RemCon.WorldMatrix.Forward * a;
                            Follower.RemCon.CustomData += $"\n{MyGPS.GPS("NewTarg", tecTarg)}";
                            Runtime.UpdateFrequency = UpdateFrequency.Update10;
                            Flag = true;
                            txt.Add("isOk" + tecTarg);
                        }
                        break;
                    case "000": if (txt.Txt != null) txt.Txt.CustomData = string.Empty;  break;
                    case "off": Follower1.Stop(); break;
                    case ">":
                        {
                            float f = 0;
                            Follower1.myThr.ForwardThrusters.ForEach(x => f += x.ThrustOverridePercentage = 40);
                            Echo($"{f}");
                        }
                            break;
                    case "?":
                        {
                            txt.AddLine("{0}", Follower1.GetStopPow(tecTarg));
                        }
                        break;
                    default: Echo("Левая команда: " + Arg); break;

                }
            }
        }
        catch (Exception e) { Echo(e.ToString()); }
    }


    public class Follower
    {
        public static IMyShipController RemCon { get; protected set; }
        Vector3D acceleration;
        static Program ParentProgram;
        public MyThrusters myThr;
        MyGyros myGyros;
        MySensors mySensors;
        MyWeapons myWeapons;

        public Follower(Program parenProg)
        {
            ParentProgram = parenProg;
            RemCon = new Selection(null).FindBlock<IMyShipController>(ParentProgram.GridTerminalSystem);
            if (RemCon == null) ParentProgram.Echo("Нет блока упаравления");
            InitSubSystems();
            acceleration = myThr.GetMaxSpeed();
        }

        private void InitSubSystems()
        {
            myThr = new MyThrusters();
            myGyros = new MyGyros(this, 3);
            mySensors = new MySensors();
            myWeapons = new MyWeapons();
        }

        public void TestHover()
        {
            Vector3D GravAccel = RemCon.GetNaturalGravity();
            //MatrixD MyMatrix = MatrixD.Invert(RemCon.WorldMatrix.GetOrientation());
            //myThr.SetThrA(Vector3D.Transform(-GravAccel, MyMatrix));

            MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
            myThr.SetThrust(VectorTransform(-GravAccel * RemCon.CalculateShipMass().TotalMass, MyMatrix));
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

        public Vector3D GetStopPowOld(Vector3D ThrVec, Vector3D Gr, bool max = false)
        {
            Vector3D res = new Vector3D();
            //X
            if (ThrVec.X > 0) res.X = Math.Min(myThr.RightThrusters.EffectivePow, ThrVec.X);
            else res.X = Math.Max(-myThr.LeftThrusters.EffectivePow, ThrVec.X);
            //Y
            if (ThrVec.Y > 0) res.Y = Math.Min(myThr.UpThrusters.EffectivePow, ThrVec.Y);
            else res.Y = Math.Max(-myThr.DownThrusters.EffectivePow, ThrVec.Y);
            //Z
            if (ThrVec.Z > 0) res.Z = Math.Min(myThr.BackwardThrusters.EffectivePow, ThrVec.Z);
            else res.Z = Math.Max(-myThr.ForwardThrusters.EffectivePow, ThrVec.Z);

            ThrVec -= Gr;
            if (!max)//Возвращаем симетричный вектор
            {
                var c = ThrVec / ThrVec.Sum;
                c /= c.AbsMax();
                res *= c;
            }
            return res;
        }
        public Vector3D GetStopPow(Vector3D ThrVec, bool max = false)
        {
            var res = new Vector3D
            {
                X = (ThrVec.X < 0 ? myThr.RightThrusters : myThr.LeftThrusters).EffectivePow,
                Y = (ThrVec.Y < 0 ? myThr.UpThrusters : myThr.DownThrusters).EffectivePow,
                Z = (ThrVec.Z < 0 ? myThr.BackwardThrusters : myThr.ForwardThrusters).EffectivePow
            };
            if (!max)//Возвращаем симетричный вектор
            {
                var c = ThrVec / ThrVec.Sum;
                c /= c.AbsMax();
                res *= c;
            }
            return res;
        }

        public void Stop()
        {
            myThr.ForEach(x => x.ThrustOverride = 0);
            Flag = false;
        }

        public double GoToPos(Vector3D Pos)
        {
            var mas = RemCon.CalculateShipMass().TotalMass;
            var speedTarg = (Pos - RemCon.GetPosition());
            var dist = speedTarg.Length();
            if (dist > MaxSpeed) speedTarg = Vector3D.Normalize(speedTarg) * MaxSpeed;

            var MatOrient = RemCon.WorldMatrix.GetOrientation();
            var GravVec = VectorTransform(-RemCon.GetNaturalGravity(), MatOrient);
            var speed = RemCon.GetShipVelocities().LinearVelocity;

            //Вектор торможения и доп. силы
            var stopVector = GetStopPow(VectorTransform(speedTarg, MatOrient)) / mas - GravVec;
            var PowVector = VectorTransform(speedTarg - speed, MatOrient);

            var coof = Math.Max(speed.Length(), 0.001);
            //coof = (dist / coof - 1) - (coof / stopVector.Length());

            //coof = (1 - coof / speedTarg.Length()) - (GetDistStop(speed.Length(), stopVector.Length()) / dist)
             coof = GetDistStop(speed.Length(), stopVector.Length()) / dist;

            txt.AddLine("D:{0:0.00} C:{1:0.0##}\nStop:{2:0.##} c :{3:0.##} м\nS{4}\nS>{5}\nSt{6}\nNs{7}\nNs_{8}",
                dist, coof, speed.Length() / stopVector.Length(), GetDistStop(speed.Length(), stopVector.Length()), 
                speed.ToString("0.###"), speedTarg.ToString("0.###"), stopVector.ToString("0.###"), PowVector.ToString("0.###"),
                (speedTarg - speed).ToString("0.###"));
           
            if (dist <= stopVector.Length()) { txt.Add("Disabled " + dist); Stop(); return 0; }

            if (coof < 0.75) PowVector = Vector3D.Normalize(PowVector) * Math.Min((1 - coof) * 100, 100);
            else PowVector = Vector3D.Normalize(stopVector) * Math.Min(coof * 100, 100);

            PowVector /= 100;
            txt.AddLine("Rvu" + PowVector.ToString("0.000"));

            myThr.SetProcThrust(PowVector);
            return dist;
        }

        public static double GetDistStop(double speed, double powStop) => Math.Pow(speed, 2) / powStop / 2;
        public static Vector3D VectorTransform(Vector3D Vec, MatrixD Orientation)
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

        public/*private*/ class MyThrusters : List<IMyThrust>
        {
            public class GroupThrusts : List<IMyThrust>
            {
                public double EffectivePow { get; protected set; }
                new public void Add(IMyThrust val) { base.Add(val); EffectivePow += val.MaxEffectiveThrust; }
                new public void Clear() { EffectivePow = 0; base.Clear(); }
                public void Recalc() { EffectivePow = 0; ForEach(x => EffectivePow += x.MaxEffectiveThrust); }
            }

            //Follower myBot;
            public GroupThrusts UpThrusters;
            public GroupThrusts DownThrusters;
            public GroupThrusts LeftThrusters;
            public GroupThrusts RightThrusters;
            public GroupThrusts ForwardThrusters;
            public GroupThrusts BackwardThrusters;

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
                return new Vector3D(
                    Math.Min(RightThrusters.EffectivePow, LeftThrusters.EffectivePow),
                    Math.Min(UpThrusters.EffectivePow, DownThrusters.EffectivePow),
                    Math.Min(ForwardThrusters.EffectivePow, BackwardThrusters.EffectivePow))
                    /RemCon.CalculateShipMass().TotalMass;
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
                Vector3 Bacw;
                RemCon.Orientation.GetMatrix(out MainLocM);

                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyThrust>(this, x => x.IsWorking);

                for (int i = 0; i < Count; i++)
                {
                    IMyThrust Thrust = this[i];
                    Thrust.Orientation.GetMatrix(out ThrLocM);
                    Bacw = ThrLocM.Backward;

                    //Y
                    if (Bacw == MainLocM.Up) UpThrusters.Add(Thrust);
                    else if (Bacw == MainLocM.Down) DownThrusters.Add(Thrust);
                    //X
                    else if (Bacw == MainLocM.Left) LeftThrusters.Add(Thrust);
                    else if (Bacw == MainLocM.Right) RightThrusters.Add(Thrust);
                    //Z
                    else if (Bacw == MainLocM.Forward) ForwardThrusters.Add(Thrust);
                    else if (Bacw == MainLocM.Backward) BackwardThrusters.Add(Thrust);
                }
            }

            //private void SetGroupThrust(List<IMyThrust> ThrList, float Thr)
            //{
            //    for (int i = 0; i < ThrList.Count; i++)
            //        ThrList[i].ThrustOverridePercentage = Thr;
            //}


            public void SetThrust(Vector3D ThrVec)
            {
                this.ForEach(x => x.ThrustOverride = 0f);
                var mas = RemCon.CalculateShipMass().TotalMass;

                ThrVec.X = ThrVec.X / (ThrVec.X > 0 ? RightThrusters : LeftThrusters).EffectivePow;
                ThrVec.Y = ThrVec.Y / (ThrVec.Y > 0 ? UpThrusters : DownThrusters).EffectivePow;
                ThrVec.Z = ThrVec.Z / (ThrVec.Z > 0 ? BackwardThrusters : ForwardThrusters).EffectivePow;

                SetProcThrust(ThrVec);
            }

            public void SetProcThrust(Vector3 ThrVec)
            {
                ForEach(x=>x.ThrustOverride = 0f);
                //X
                if (ThrVec.X > 0) RightThrusters.ForEach(x => x.ThrustOverridePercentage = ThrVec.X);
                else LeftThrusters.ForEach(x => x.ThrustOverridePercentage = -ThrVec.X);

                //Y
                if (ThrVec.Y > 0) UpThrusters.ForEach(x => x.ThrustOverridePercentage = ThrVec.Y);
                else DownThrusters.ForEach(x => x.ThrustOverridePercentage = -ThrVec.Y);

                //Z
                if (ThrVec.Z > 0) BackwardThrusters.ForEach(x => x.ThrustOverridePercentage = ThrVec.Z);
                else ForwardThrusters.ForEach(x => x.ThrustOverridePercentage = -ThrVec.Z);

                ThrVec *= 100;

                if (ThrVec.X > 0) txt.Add(string.Format("R:{0:0.00} % ", ThrVec.X)); else txt.Add(string.Format("L:{0:0.00} % ", -ThrVec.X));
                if (ThrVec.Y > 0) txt.Add(string.Format("U:{0:0.00} % ", ThrVec.Y)); else txt.Add(string.Format("D:{0:0.00} % ", -ThrVec.Y));
                if (ThrVec.Z > 0) txt.Add(string.Format("B:{0:0.00} %", ThrVec.Z)); else txt.Add(string.Format("F:{0:0.00} %", -ThrVec.Z));
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
            if (Txt != null) { Txt.WritePublicText(text); Txt.CustomData += "\n\n" + text; }
        }
        public void Show(MyGridProgram GS, bool ToConsole, bool Added)
        {
            this.Added = Added;
            this.ToConsole = ToConsole;
            Show(GS);
        }

        public ShowMes Add(string text) { buf.Add(text); return this; }
        public ShowMes AddLine(string text) => Add(text + "\n");
        public ShowMes AddLine(string format, params object[] args) => Add(string.Format(format, args)+ "\n");
        public ShowMes AddFromLine(string text) => Add("\n" + text);
        public ShowMes AddFromLine(string format, params object[] args) => Add("\n" + string.Format(format, args));

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
