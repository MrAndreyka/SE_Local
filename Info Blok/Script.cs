/*----------------------------------------------------------------------  
        AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com  
        When using and disseminating information about the authorship is obligatory  
        При использовании и распространении информация об авторстве обязательна  
        ----------------------------------------------------------------------*/

        void Main(string argument)
    {
        var ms = argument.Split('>');

        IMyTerminalBlock RM = GridTerminalSystem.GetBlockWithName(ms[0]);
        if (RM == null)
        {
            Echo("Не найден блок " + ms[0]);
            return;
        }

        IMyTerminalBlock TP = null;
        if (ms.GetLength(0) > 1) TP = GridTerminalSystem.GetBlockWithName(ms[1]);

        var s = new StringBuilder(RM.CustomNameWithFaction + "\n");

        s.Append(RM.GetType().ToString() + "\n\nActions:");

        List<ITerminalAction> acts = new List<ITerminalAction>();
        RM.GetActions(acts);
        int i;
        for (i = 0; i < acts.Count; i++) s.AppendLine(acts[i].Id + " - " + acts[i].Name.ToString());

        s.AppendLine("\nPropertys:");
        List<ITerminalProperty> pros = new List<ITerminalProperty>();
        RM.GetProperties(pros);

        for (i = 0; i < pros.Count; i++)
        {
            var tv = pros[i];
            s.Append(tv.Id + " - " + tv.TypeName + " _ ");
            switch (tv.TypeName)
            {
                case "Boolean": s.AppendLine(RM.GetValueBool(tv.Id).ToString()); break;
                case "Single": s.AppendLine(RM.GetValueFloat(tv.Id).ToString()); break;
                case "Color": s.AppendLine(RM.GetValueColor(tv.Id).ToString()); break;
                default: s.AppendLine("[unspecified type]"); break;
            }

        }

        s.AppendLine("\nMax:  " + RM.Max + " Min:  " + RM.Min + " = " + (RM.Max - RM.Min));
        s.AppendLine("DisplayName:  " + RM.DisplayName);
        s.AppendLine("DisplayNameText:  " + RM.DisplayNameText);
        s.AppendLine("DefinitionDisplayNameText:  " + RM.DefinitionDisplayNameText);
        s.AppendLine("Name:  " + RM.Name);
        s.AppendLine("DisassembleRatio:  " + RM.DisassembleRatio);
        s.AppendLine("DetailedInfo:  " + RM.DetailedInfo);
        s.AppendLine("CustomInfo:  " + RM.CustomInfo);
        s.AppendLine("CustomNameWithFaction:  " + RM.CustomNameWithFaction);
        s.AppendLine("BlockDefinition:  " + RM.BlockDefinition);

        var ps = RM.GetPosition();
        s.AppendLine("\n" + MyGPS.GPS("Position", ps));
        s.AppendLine(MyGPS.GPS("Top", RM.WorldMatrix.Up, ps, 10));
        s.AppendLine(MyGPS.GPS("Left", RM.WorldMatrix.Left, ps, 10));
        s.AppendLine(MyGPS.GPS("Forward", RM.WorldMatrix.Forward, ps, 10));

        if (RM is IMyLargeGatlingTurret)
        {
            var gt = RM as IMyLargeGatlingTurret;
            s.AppendLine("IsUnderControl:  " + gt.IsUnderControl);
            s.AppendLine("A:" + MathHelper.ToDegrees(gt.Azimuth) + " Z: " + MathHelper.ToDegrees(gt.Elevation));
            var vf = RM.WorldMatrix.Forward;
            var mm = MatrixD.CreateFromAxisAngle(RM.WorldMatrix.Right, gt.Elevation);
            Vector3D.Transform(ref vf, ref mm, out vf);
            mm = MatrixD.CreateFromAxisAngle(RM.WorldMatrix.Up, gt.Azimuth);
            Vector3D.Transform(ref vf, ref mm, out vf);
            s.AppendLine(MyGPS.Vec_GPS("Targ", vf, ps, 800));
        }

        s.AppendLine("\nInventory - " + (i = RM.InventoryCount));
        while (i > 0) InfoInventory(RM.GetInventory(--i), s);

        if (ms.Length > 2) TP.CustomData += "\n" + s.ToString();
        else if (TP != null) (TP as IMyTextPanel).WritePublicText(s.ToString());
        else Echo(s.ToString());
    }

    void InfoInventory(IMyInventory Inv, StringBuilder s)
    {
        s.AppendLine(string.Format("Size: {0} Mass: {1} MVol: {2} Vol: {3} ", Inv.Size, Inv.CurrentMass, Inv.MaxVolume, Inv.CurrentVolume));
        s.AppendLine("Type " + Inv.GetType());
        List<IMyInventoryItem> L = Inv.GetItems();
        L.ForEach(it => {
            s.AppendLine(string.Format("Content: ToString: {0} Type: {1} TypeName: {2} DefId: {3}", it.Content, it.Content.TypeId, it.Content.SubtypeId, it.GetDefinitionId()));
            s.AppendLine(string.Format("Content: Int: {0} Type: {1} RawVal: {2} ToString: {3}\n", it.Amount.ToIntSafe(), it.Amount.GetType(), it.Amount.RawValue, it.Amount.ToString()));
        });
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