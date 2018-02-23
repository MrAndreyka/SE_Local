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

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class Program : MyGridProgram
{
    /*--------------------------------------------------------------------------    
    AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: MyAndrey_ka@mail.ru     
    When using and disseminating information about the authorship is obligatory    
    ѕри использовании и распространении информаци€ об авторстве об€зательна    
    ----------------------------------------------------------------------*/

    Program()
    {
        if (!string.IsNullOrEmpty(Me.CustomData))
            SetAtributes(new string[] { "Init_" + Me.CustomData });
    }

    IMyAssembler asm = null;
    IMyTextPanel TP = null;
    IMyInventory inv = null;

    // ---------------------------------- MAIN ---------------------------------------------  
    void Main(string argument)
    {
        
    }
    // ---------------------------------- end MAIN ---------------------------------------------   

  static class MyGPS
{
	public static string GPS(string Name, Vector3D Val)
	{ return string.Format("GPS:{0}:{1:0.#}:{2:0.#}:{3:0.#}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
	public static string GPS(string Name, Vector3D Val, Vector3D Pos, int dist) { return GPS(Name, Pos + Val * dist); }
	public static string Vec_GPS(string Name, Vector3D Val, Vector3D Pos, int dist)
	{ Pos += Val * dist; return Name + Val + "\n" + GPS(Name, Pos + Val * dist); }
	public static bool TryParse(string vector, out Vector3 res)
	{
		var p = vector.Split(':');
		if (p.GetLength(0) == 6)
		{
			float x, y, z;
			if (float.TryParse(p[2], out x) && float.TryParse(p[3], out y) && float.TryParse(p[4], out z))
				res = new Vector3(x, y, z);
			else
				res = Vector3.Zero;
		}
		else res = Vector3.Zero;
		return res != Vector3.Zero;
	}
}




public class selection {byte key=0;public string Val;public bool inv;
	public selection(string val){SetSel(val);}
	public void SetSel(string val){
		Val = ""; key = 0;
		if (string.IsNullOrEmpty(val)) return;
		inv = val.StartsWith("!");
		if (inv) val = val.Remove(0, 1);
		if (string.IsNullOrEmpty(val)) return;
		int Pos = val.IndexOf('*', 0, 1) + val.LastIndexOf('*', val.Length - 1, 1) + 2;
		if (Pos == 0) Pos = 0;
		else if (Pos == 1) Pos = 1;
		else if (Pos == val.Length) Pos = 2;
		else Pos = Pos < 4 ? 1 : 3;
		if (Pos != 0){if (Pos != 2) val = val.Remove(0, 1);
			if (Pos != 1) val = val.Remove(val.Length - 1, 1);}
		Val = val;key = (byte)Pos;}
		
	public bool complies(string str){
		if (string.IsNullOrEmpty(Val)) return !inv;
		switch (key){
			case 0: return str == Val != inv;
			case 1: return str.EndsWith(Val) != inv;
			case 2: return str.StartsWith(Val) != inv;
			case 3: return str.Contains(Val) != inv;}
		return false;}

	public Type FindBlock<Type>(IMyGridTerminalSystem TB, Func<Type, bool> Fp = null) where Type : class{
		List<Type> res = new List<Type>();
		TB.GetBlocksOfType<Type>(res, x => complies((x as IMyTerminalBlock).CustomName) && (Fp == null || Fp(x)));
		return res.Count == 0 ? null : res[0];}
	public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null){
		TB.SearchBlocksOfName(inv ? "" : Val, res, x => complies(x.CustomName) && (Fp == null || Fp(x)));}
	public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
		{TB.GetBlocksOfType<Type>(res, x => complies((x as IMyTerminalBlock).CustomName) && (Fp == null || Fp(x)));}
}

