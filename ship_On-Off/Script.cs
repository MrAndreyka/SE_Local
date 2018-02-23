// CustomDate 
//  Коннектор 
//  Таймер включения 
//  Таймер выключения 
//  Группа вылючения

IMyShipConnector St;
IMyTimerBlock Go, Go2;
List<IMyTerminalBlock> Lst;

Program() {
	var a = Me.CustomData.Split('\n');
	var i = a.GetLength(0);
	if (i < 3) { Echo("Ошибка инициализации, не хватает параметров"); return; }

	St = GridTerminalSystem.GetBlockWithName(a[0]) as IMyShipConnector;
	Go = GridTerminalSystem.GetBlockWithName(a[1]) as IMyTimerBlock;
	Go2 = GridTerminalSystem.GetBlockWithName(a[2]) as IMyTimerBlock;
	if (i > 3) new selection(a[3]).FindBlocks(GridTerminalSystem, Lst);
}

void Main(string s)
{
	if (St == null)
		return;

	if (St.Status == MyShipConnectorStatus.Connectable)
		Go.ApplyAction("TriggerNow");
	else if (St.Status == MyShipConnectorStatus.Connected)
		Go2.ApplyAction("TriggerNow");
	else if (Lst != null)
		Lst.ForEach(x=> x.ApplyAction("OnOff_Off"));
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

