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

    static Vector3D VGr;
    static IMyShipController RemCon = null;
    static PID Giros = new PID();
    static Vector3D Fr;
    const float mnoj = 1;
    static IMyTextPanel Txt;

    Program()
    {
        RemCon = new Selection(null).FindBlock<IMyShipController>(GridTerminalSystem);
        Fr = Vector3D.Reject(RemCon.WorldMatrix.Forward, Vector3D.Normalize(RemCon.GetNaturalGravity()));
        Txt = new Selection(null).FindBlock<IMyTextPanel>(GridTerminalSystem);
        if (Txt != null) Txt.CustomData = MyGPS.GPS("F", Fr, RemCon.GetPosition(), 1000);
        GridTerminalSystem.GetBlocksOfType<IMyGyro>(Giros);
        Runtime.UpdateFrequency = UpdateFrequency.Update10;
    }

    void Main()
    {
        VGr = Vector3D.Normalize(RemCon.GetNaturalGravity());

        float Y = GetAngel(VGr, RemCon.WorldMatrix.Forward, Fr),
            P = GetAngel(RemCon.WorldMatrix.Left, RemCon.WorldMatrix.Down, VGr),
            R = GetAngel(Vector3D.Normalize(Fr), RemCon.WorldMatrix.Down, VGr);
        var s = $"{Y}\n{P}\n{R}\nYow: {MathHelper.ToDegrees(Y):0.00}°\nPich: {MathHelper.ToDegrees(P):0.00}°\nRoll: {MathHelper.ToDegrees(R):0.00}°";
        Giros.Drive(Y* mnoj, P* mnoj, R* mnoj, RemCon.WorldMatrix);

        Y = (float)RemCon.WorldMatrix.Forward.Dot(Vector3D.Reject(Fr, VGr));
        P = (float)RemCon.WorldMatrix.Down.Dot(Vector3D.Reject(VGr, RemCon.WorldMatrix.Left));
        R = (float)RemCon.WorldMatrix.Down.Dot(Vector3D.Reject(VGr, Fr));
        s += $"\n{Y}\n{P}\n{R}\nYow: {MathHelper.ToDegrees(Y):0.00}°\nPich: {MathHelper.ToDegrees(P):0.00}°\nRoll: {MathHelper.ToDegrees(R):0.00}°";

        if (Txt == null) Echo(s); else Txt.WritePublicText(s);
        
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
        var res = MyMath.AngleBetween(VDirect, tm);
        return tm.Dot(-VDirect) > 0 ?res:-res;
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
           
            return Vdr;
        }
        public void Drive(double yaw_speed, double pitch_speed, double roll_speed, MatrixD shipMatrix)
        {
            if (Count == 0) return;
            var relativeRotationVec = Vector3D.TransformNormal(new Vector3D(-pitch_speed, yaw_speed, roll_speed), shipMatrix);
            foreach (var thisGyro in this)
            {
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));
               // thisGyro.GyroOverride = true;
                thisGyro.Pitch = (float)transformedRotationVec.X - (transformedRotationVec.X > 0.5 ? thisGyro.Pitch / 3 : 0);
                thisGyro.Yaw = (float)transformedRotationVec.Y - (transformedRotationVec.Y > 0.5 ? thisGyro.Yaw / 3 : 0);
                thisGyro.Roll = (float)transformedRotationVec.Z - (transformedRotationVec.Z > 0.5 ? thisGyro.Roll / 3 : 0);
            }
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
