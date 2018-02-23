/*----------------------------------------------------------------------  
    AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com  
    When using and disseminating information about the authorship is obligatory  
    При использовании и распространении информация об авторстве обязательна  
    ----------------------------------------------------------------------*/
    public void Main(string argument)
    {
        string rel = "", mask = "";
        bool m = false, so = argument.StartsWith("~");//ShowOnly 

        if (so) argument = argument.Remove(0, 1);
        int pos = argument.IndexOf(':');
        if (pos >= 0)
        {
            mask = argument.Substring(0, pos);
            argument = argument.Substring(pos + 1);
        }

        pos = argument.IndexOf('=');
        if (pos >= 0) // Замена  
        {
            rel = argument.Substring(pos + 1);
            argument = argument.Remove(pos);
            m = true;
        }
        else if (argument[0] == '-') // Удаление (та же замена)  
        { argument = argument.Substring(1); m = true; }
        else if (!argument.Contains("{")) argument += "{0}";

        if (m && mask == "") mask = "*" + argument + "*";


        List<IMyTerminalBlock> List = new List<IMyTerminalBlock>();
        new Selection(mask).FindBlocks(GridTerminalSystem, List);
        if (List.Count == 0) Echo($"Не найдены блоки по маске: {mask}");
        var DM = new Dictionary<Type, int>();

        for (int i = 0; i < List.Count; i++)
        {
            var Bl = List[i];
            if (m) mask = Bl.CustomName.Replace(argument, rel);
            else
            {
                if (!DM.TryGetValue(Bl.GetType(), out pos)) pos = 0;
                mask = string.Format(argument, Bl.CustomName, pos, Bl.DefinitionDisplayNameText).Trim();
                DM[Bl.GetType()] = ++pos;
            }

            if (so) Echo(Bl.CustomName + " => " + mask);
            else Bl.CustomName = mask;
        }
    }

    public class Selection
    {
        byte key = 0; string Val;
        public bool inv;
        public string Value { get { return Val; } set { SetSel(value); } }

        public Selection(string val) { Value = val; }
        public Selection Change(string val) { Value = val; return this; }
        void SetSel(string val)
        {
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
            if (Pos != 0)
            {
                if (Pos != 2) val = val.Remove(0, 1);
                if (Pos != 1) val = val.Remove(val.Length - 1, 1);
            }
            Val = val; key = (byte)Pos;
        }
        public override string ToString() { return inv ? "!" : ""; }

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
            TB.GetBlocksOfType<Type>(res, x => fs ? false : fs = (Complies((x as IMyTerminalBlock).CustomName) && (Fp == null || Fp(x))));
            return res.Count == 0 ? null : res[0];
        }
        public void FindBlocks(IMyGridTerminalSystem TB, List<IMyTerminalBlock> res, Func<IMyTerminalBlock, bool> Fp = null)
        {
            TB.SearchBlocksOfName(inv ? "" : Val, res, x => Complies(x.CustomName) && (Fp == null || Fp(x)));
        }
        public void FindBlocks<Type>(IMyGridTerminalSystem TB, List<Type> res, Func<Type, bool> Fp = null) where Type : class
        { TB.GetBlocksOfType<Type>(res, x => Complies((x as IMyTerminalBlock).CustomName) && (Fp == null || Fp(x))); }
    }