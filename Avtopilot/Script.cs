 
    int TickCount; 
    int Clock = 10; 
    string ShipName = "Roach1"; 
    string NewName = "Roach1"; 
    float GyroMult = 0.5f; 
    int CriticalMass = 800000; 
    float AlignAccelMult = 0.3f; 
    float ReturnOnCharge = 0.2f; 
    float TargetSize = 300; 
    //---------------------             
 
 
    IMyTimerBlock Timer; 
 
    MyDriller thisDriller; 
    public static class Commands 
    { 
        public const int Idle = 0; 
        public const int SaveConnector1 = 1; 
        public const int SaveConnector2 = 2; 
        public const int Lock = 3; 
        public const int Complete = 4; 
        public const int StartTransfer = 5; 
        public const int Pause = 6; 
 
        public const int UnDock1 = 20; 
        public const int UnDock2 = 21; 
        public const int ToBase1 = 22; 
        public const int Dock1 = 23; 
        public const int Docked1 = 24; 
        public const int ToBase2 = 25; 
        public const int Dock2 = 26; 
        public const int Docked2 = 27; 
        public const int BaseOperations1 = 28; 
        public const int BaseOperations2 = 29; 
    } 
 
    void Main(string argument) 
    { 
        if (thisDriller == null) 
            thisDriller = new MyDriller(ShipName, this); 
        if (Timer == null) 
            Timer = GridTerminalSystem.GetBlockWithName(thisDriller.MyName + "TimerClock") as IMyTimerBlock; 
        TickCount++; 
 
        if (argument != "") 
        { 
            TickCount = 0; 
            switch (argument) 
            { 
                case "SaveConnector1": 
                    { 
                        if (thisDriller.Paused) 
                            thisDriller.Pause(); 
                        thisDriller.Command = Commands.SaveConnector1; 
                        break; 
                    } 
                case "SaveConnector2": 
                    { 
                        if (thisDriller.Paused) 
                            thisDriller.Pause(); 
                        thisDriller.Command = Commands.SaveConnector2; 
                        break; 
                    } 
                case "StartTransfer": 
                    { 
                        if (thisDriller.Paused) 
                            thisDriller.Pause(); 
                        thisDriller.Command = Commands.StartTransfer; 
                        break; 
                    } 
 
                case "Pause": 
                    { 
                        thisDriller.Pause(); 
                        break; 
                    } 
                case "Rename": 
                    { 
                        thisDriller.Rename(ShipName, NewName); 
                        if (!thisDriller.Paused) 
                            thisDriller.Pause(); 
                        break; 
                    } 
                case "PlanetCenter": 
                    { 
                        thisDriller.FindPlanetCenter(); 
                        if (!thisDriller.Paused) 
                            thisDriller.Pause(); 
                        break; 
                    } 
                case "SetFlyHeight": 
                    { 
                        thisDriller.SetFlyHeight(); 
                        if (!thisDriller.Paused) 
                            thisDriller.Pause(); 
                        break; 
                    } 
                case "DisplayHeight": 
                    { 
                        thisDriller.DisplayHeight(); 
                        if (!thisDriller.Paused) 
                            thisDriller.Pause(); 
                        break; 
                    } 
                case "GoHome": 
                    { 
                        thisDriller.GoHome = true; 
                        if (thisDriller.Paused) 
                            thisDriller.Pause(); 
                        break; 
                    } 
                case "StayHome": 
                    { 
                        thisDriller.GoHome = true; 
                        thisDriller.StayHome = true; 
                        if (thisDriller.Paused) 
                            thisDriller.Pause(); 
                        break; 
                    } 
 
                default: 
                    break; 
            } 
        } 
 
        if (!thisDriller.Paused) 
        { 
            if ((TickCount % Clock) == 0) 
            { 
                thisDriller.Update(); 
            } 
            Timer.GetActionWithName("TriggerNow").Apply(Timer); 
        } 
    } 
 
    public class MyDriller 
    { 
        private MyNavigation navBlock; 
        private MyThrusters thrustBlock; 
        private MyGyros gyroBlock; 
        private MyCargo cargoBlock; 
        private MyConnector connectorBlock; 
        private MyBatteries batteryBlock; 
        public int Command { get; set; } 
        public string MyName { get; private set; } 
        internal static Program ParentProgram; 
        public float ShipMass { get; private set; } 
        public bool EmergencyReturn = false; 
        public bool GoHome = false; 
        public bool StayHome = false; 
        public bool Paused { get; private set; } 
        public int CurrentStatus { get; set; } 
        public MyDriller(string DrillerName, Program MyProg) 
        { 
            MyName = DrillerName; 
            ParentProgram = MyProg; 
            InitSubSystems(); 
        } 
        public void InitSubSystems() 
        { 
            thrustBlock = new MyThrusters(this); 
            gyroBlock = new MyGyros(this); 
            cargoBlock = new MyCargo(this); 
            connectorBlock = new MyConnector(this); 
            batteryBlock = new MyBatteries(this); 
            navBlock = new MyNavigation(this); 
        } 
 
        public void FindPlanetCenter() 
        { 
            navBlock.FindPlanetCenter(); 
        } 
        public void DisplayHeight() 
        { 
            navBlock.DisplayHeight(); 
        } 
        public void SetFlyHeight() 
        { 
            navBlock.SetFlyHeight(); 
        } 
        public void Rename(string OldName, string NewName) 
        { 
            List<IMyTerminalBlock> blockTemp = new List<IMyTerminalBlock>(); 
            ParentProgram.GridTerminalSystem.SearchBlocksOfName(OldName, blockTemp); 
            for (int i = 0; i < blockTemp.Count; i++) 
            { 
                IMyTerminalBlock block = blockTemp[i] as IMyTerminalBlock; 
                block.CustomName = block.CustomName.Replace(OldName, NewName); 
            } 
            MyName = NewName; 
            InitSubSystems(); 
        } 
        public void TextOutput(string TP, string Output = "") 
        { 
            IMyTextPanel ScrObj = ParentProgram.GridTerminalSystem.GetBlockWithName(MyName + TP) as IMyTextPanel; 
            if (ScrObj != null) 
            { 
                if (Output != "") 
                { 
                    ScrObj.WritePublicText(Output); 
                } 
                ScrObj.GetActionWithName("OnOff_On").Apply(ScrObj); 
            } 
        } 
        private static void TurnGroup(string t, string OnOff) 
        { 
            var GrItems = GetBlocksFromGroup(t); 
            for (int i = 0; i < GrItems.Count; i++) 
            { 
                var GrItem = GrItems[i] as IMyTerminalBlock; 
                GrItem.GetActionWithName("OnOff_" + OnOff).Apply(GrItem); 
            } 
        } 
        private static List<IMyTerminalBlock> GetBlocksFromGroup(string group) 
        { 
            var blocks = new List<IMyTerminalBlock>(); 
            ParentProgram.GridTerminalSystem.SearchBlocksOfName(group, blocks); 
            if (blocks != null) 
            { return blocks; } 
            throw new Exception("GetBlocksFromGroup: Group \"" + group + "\" not found"); 
        } 
 
        public void Pause() 
        { 
            IMyTextPanel ScrObj = ParentProgram.GridTerminalSystem.GetBlockWithName(MyName + "TP_Icon") as IMyTextPanel; 
            if (Paused) 
            { 
                ParentProgram.Timer.ApplyAction("Start"); 
                Paused = false; 
                ScrObj.GetActionWithName("OnOff_On").Apply(ScrObj); 
            } 
            else 
            { 
                gyroBlock.SetOverride(false, "", 1); 
                thrustBlock.SetOverridePercent("", 0); 
                ParentProgram.Timer.ApplyAction("Stop"); 
                connectorBlock.Turn("On"); 
                Paused = true; 
                ScrObj.GetActionWithName("OnOff_Off").Apply(ScrObj); 
            } 
            navBlock.SaveToStorage(); 
        } 
 
        private void SequencerTransfer() 
        { 
            if (Command == Commands.StartTransfer) 
            { 
                CurrentStatus = Commands.ToBase1; 
                Command = Commands.ToBase1; 
                GoHome = false; StayHome = false; 
            } 
            else 
                switch (CurrentStatus) 
                { 
                    case Commands.UnDock2: 
                        if (navBlock.UnDock(navBlock.DockMatrix2)) 
                        { 
                            CurrentStatus = Commands.ToBase1; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    case Commands.ToBase1: 
                        if (navBlock.ToBase(navBlock.DockMatrix1)) 
                        { 
                            CurrentStatus = Commands.Dock1; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    case Commands.Dock1: 
                        if (navBlock.Dock(navBlock.DockMatrix1)) 
                        { 
                            CurrentStatus = Commands.Docked1; 
                            //navBlock.SaveToStorage();             
                        } 
                        break; 
                    case Commands.Docked1: 
                        if (connectorBlock.Locked) 
                        { 
                            CurrentStatus = Commands.BaseOperations1; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    case Commands.BaseOperations1: 
                        if (navBlock.UnloadAndRecharge(true)) 
                        { 
                            if (StayHome) 
                            { 
                                CurrentStatus = Commands.Complete; 
                                batteryBlock.Recharge(false); 
                                thrustBlock.Turn("On"); 
                                thrustBlock.SetOverridePercent("U", 0); 
                                thrustBlock.SetOverridePercent("R", 0); 
                                thrustBlock.SetOverridePercent("L", 0); 
                                thrustBlock.SetOverridePercent("F", 0); 
                                thrustBlock.SetOverridePercent("B", 0); 
                            } 
                            else 
                                CurrentStatus = Commands.UnDock1; 
                            if (GoHome) GoHome = false; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
 
                    case Commands.UnDock1: 
                        if (navBlock.UnDock(navBlock.DockMatrix1)) 
                        { 
                            CurrentStatus = Commands.ToBase2; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    case Commands.ToBase2: 
                        if (navBlock.ToBase(navBlock.DockMatrix2)) 
                        { 
                            CurrentStatus = Commands.Dock2; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    case Commands.Dock2: 
                        if (navBlock.Dock(navBlock.DockMatrix2)) 
                        { 
                            CurrentStatus = Commands.Docked2; 
                            //navBlock.SaveToStorage();             
                        } 
                        break; 
                    case Commands.Docked2: 
                        if (connectorBlock.Locked) 
                        { 
                            CurrentStatus = Commands.BaseOperations2; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    case Commands.BaseOperations2: 
                        if (navBlock.UnloadAndRecharge(false)) 
                        { 
                            if (StayHome) 
                            { 
                                CurrentStatus = Commands.Complete; 
                                batteryBlock.Recharge(false); 
                                thrustBlock.Turn("On"); 
                                thrustBlock.SetOverridePercent("U", 0); 
                                thrustBlock.SetOverridePercent("R", 0); 
                                thrustBlock.SetOverridePercent("L", 0); 
                                thrustBlock.SetOverridePercent("F", 0); 
                                thrustBlock.SetOverridePercent("B", 0); 
                            } 
                            else 
                                CurrentStatus = Commands.UnDock2; 
                            if (GoHome) GoHome = false; 
                            navBlock.SaveToStorage(); 
                        } 
                        break; 
                    default: 
                        break; 
 
                } 
        } 
 
 
 
        public void Update() 
        { 
            if (!Paused) 
            { 
                cargoBlock.Update(); 
                navBlock.Update(); 
                thrustBlock.Update(); 
                batteryBlock.Update(); 
                if (Command == Commands.SaveConnector1) 
                { 
                    if (connectorBlock.Connector.Status == MyShipConnectorStatus.Connected) 
                    { 
                        navBlock.SetDockMatrix1(); 
                        navBlock.SaveToStorage(); 
                        Command = Commands.Pause; 
                    } 
                } 
                else if (Command == Commands.SaveConnector2) 
                { 
                    if (connectorBlock.Connector.Status == MyShipConnectorStatus.Connected) 
                    { 
                        navBlock.SetDockMatrix2(); 
                        navBlock.SaveToStorage(); 
                        Command = Commands.Pause; 
                    } 
                } 
                else if (Command == Commands.Pause) 
                { 
                    Pause(); 
                    navBlock.SaveToStorage(); 
                } 
                else 
                { 
                    SequencerTransfer(); 
                } 
            } 
        } 
 
        private class MyThrusters 
        { 
            private List<IMyTerminalBlock> Thrusts; 
            public float TotalMass { get; private set; } 
            //public float ThrustMultiplier { get;private set;}             
            private MyDriller myDriller; 
            private static string Prefix = "Thr"; 
            public float g { get; private set; } 
            private IMyShipController ShipControl; 
 
            private Matrix ControlLocM; 
 
            public float UMaxT { get; private set; } 
            public float DMaxT { get; private set; } 
            public float FMaxT { get; private set; } 
            public float BMaxT { get; private set; } 
            public float RMaxT { get; private set; } 
            public float LMaxT { get; private set; } 
 
            public float XMaxA { get; private set; } 
            public float YMaxA { get; private set; } 
            public float ZMaxA { get; private set; } 
 
 
 
            public MyThrusters(MyDriller mdr) 
            { 
                myDriller = mdr; 
                Thrusts = new List<IMyTerminalBlock>(); 
                ShipControl = ParentProgram.GridTerminalSystem.GetBlockWithName(myDriller.MyName + "RemCon") as IMyShipController; 
                ShipControl.Orientation.GetMatrix(out ControlLocM); 
            } 
 
            public void Update() 
            { 
                UMaxT = 0; 
                DMaxT = 0; 
                FMaxT = 0; 
                BMaxT = 0; 
                RMaxT = 0; 
                LMaxT = 0; 
 
                Matrix ThrLocM = new Matrix(); 
 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myDriller.MyName + Prefix, Thrusts); 
                for (int i = 0; i < Thrusts.Count; i++) 
                { 
                    IMyThrust Thrust = Thrusts[i] as IMyThrust; 
                    if (Thrust != null) 
                    { 
                        Thrust.SetValue("Override", 0f); 
                        Thrust.Orientation.GetMatrix(out ThrLocM); 
                        if (ThrLocM.Forward == ControlLocM.Up) 
                        { 
                            DMaxT += Thrust.MaxEffectiveThrust; 
                        } 
                        else if (ThrLocM.Forward == ControlLocM.Down) 
                        { 
                            UMaxT += Thrust.MaxEffectiveThrust; 
                        } 
                        else if (ThrLocM.Forward == ControlLocM.Forward) 
                        { 
                            BMaxT += Thrust.MaxEffectiveThrust; 
                        } 
                        else if (ThrLocM.Forward == ControlLocM.Backward) 
                        { 
                            FMaxT += Thrust.MaxEffectiveThrust; 
                        } 
                        else if (ThrLocM.Forward == ControlLocM.Right) 
                        { 
                            LMaxT += Thrust.MaxEffectiveThrust; 
                        } 
                        else if (ThrLocM.Forward == ControlLocM.Left) 
                        { 
                            RMaxT += Thrust.MaxEffectiveThrust; 
                        } 
                    } 
                } 
 
                g = (float)ShipControl.GetNaturalGravity().Length(); 
                TotalMass = ShipControl.CalculateShipMass().PhysicalMass; 
                //myDriller.TextOutput("TP_Rock", TotalMass.ToString());   
                YMaxA = Math.Min(UMaxT / TotalMass - g, DMaxT / TotalMass + g); 
                ZMaxA = Math.Min(FMaxT, BMaxT) / TotalMass; 
                XMaxA = Math.Min(RMaxT, LMaxT) / TotalMass; 
            } 
 
            public void SetOverridePercent(string axis, float OverrideValue) 
            { 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myDriller.MyName + Prefix + axis, Thrusts); 
                for (int i = 0; i < Thrusts.Count; i++) 
                { 
                    IMyThrust Thrust = Thrusts[i] as IMyThrust; 
                    if (Thrust != null) 
                    { 
                        Thrust.SetValue("Override", OverrideValue); 
                        // ThrustMultiplier = Thrust.GetValue<float>("ThrustMultiplier");             
                    } 
                } 
            } 
 
            public void SetOverrideN(string axis, float OverrideValue) 
            { 
                float MaxThrust = 0; 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myDriller.MyName + Prefix + axis, Thrusts); 
 
                if (axis == "U") MaxThrust = UMaxT; 
                else if (axis == "D") MaxThrust = DMaxT; 
                else if (axis == "F") MaxThrust = FMaxT; 
                else if (axis == "B") MaxThrust = BMaxT; 
                else if (axis == "R") MaxThrust = RMaxT; 
                else if (axis == "L") MaxThrust = LMaxT; 
 
                for (int i = 0; i < Thrusts.Count; i++) 
                { 
                    IMyThrust Thrust = Thrusts[i] as IMyThrust; 
                    if (Thrust != null) 
                    { 
                        //if (axis == "U") myDriller.TextOutput("TP_Rock", OverrideValue.ToString());   
                        if (OverrideValue == 0f) 
                        { 
                            Thrust.SetValue("Override", 0f); 
                        } 
                        else 
                        { 
                            Thrust.SetValue("Override", Math.Max(OverrideValue * 100 / MaxThrust, 2)); 
                        } 
                    } 
                } 
            } 
 
            public void SetOverrideAccel(string axis, float OverrideValue) 
            { 
                switch (axis) 
                { 
 
                    case "U": 
                        OverrideValue += g; 
                        if (OverrideValue < 0) 
                        { 
                            axis = "D"; 
                            OverrideValue = -OverrideValue; 
                        } 
                        break; 
                    case "L": 
                        if (OverrideValue < 0) 
                        { 
                            axis = "R"; 
                            OverrideValue = -OverrideValue; 
                        } 
                        break; 
                    case "R": 
                        if (OverrideValue < 0) 
                        { 
                            axis = "L"; 
                            OverrideValue = -OverrideValue; 
                        } 
                        break; 
                    case "F": 
                        if (OverrideValue < 0) 
                        { 
                            axis = "B"; 
                            OverrideValue = -OverrideValue; 
                        } 
                        break; 
                    case "B": 
                        if (OverrideValue < 0) 
                        { 
                            axis = "F"; 
                            OverrideValue = -OverrideValue; 
                        } 
                        break; 
                } 
                SetOverrideN(axis, OverrideValue * TotalMass); 
                //myDriller.TextOutput("TP_Rock", OverrideValue.ToString());             
            } 
 
            public void Turn(string OnOff) 
            { 
                TurnGroup(myDriller.MyName + "Thr", OnOff); 
            } 
        } 
 
        private class MyGyros 
        { 
            private MyDriller myDriller; 
            private static string Prefix = "Gyro"; 
 
            public MyGyros(MyDriller mdr) 
            { 
                myDriller = mdr; 
            } 
 
            public void Turn(string OnOff) 
            { 
                TurnGroup(myDriller.MyName + "Gyro", OnOff); 
            } 
            public void SetOverride(bool OverrideOnOff = true, string axis = "", float OverrideValue = 0, float Power = 1) 
            { 
                var Gyros = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myDriller.MyName + Prefix, Gyros); 
                for (int i = 0; i < Gyros.Count; i++) 
                { 
                    IMyGyro Gyro = Gyros[i] as IMyGyro; 
                    if (Gyro != null) 
                    { 
                        if (((!Gyro.GyroOverride) && OverrideOnOff) || ((Gyro.GyroOverride) && !OverrideOnOff)) 
                            Gyro.ApplyAction("Override"); 
 
                        Gyro.SetValue("Power", Power); 
                        if (axis != "") 
                            Gyro.SetValue(axis, OverrideValue); 
                    } 
                } 
            } 
            public void SetOverride(bool OverrideOnOff, Vector3 settings, float Power = 1) 
            { 
                var Gyros = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName(myDriller.MyName + Prefix, Gyros); 
                for (int i = 0; i < Gyros.Count; i++) 
                { 
                    IMyGyro Gyro = Gyros[i] as IMyGyro; 
                    if (Gyro != null) 
                    { 
                        if ((!Gyro.GyroOverride) && OverrideOnOff) 
                            Gyro.ApplyAction("Override"); 
                        Gyro.SetValue("Power", Power); 
                        Gyro.SetValue("Yaw", settings.GetDim(0)); 
                        Gyro.SetValue("Pitch", settings.GetDim(1)); 
                        Gyro.SetValue("Roll", settings.GetDim(2)); 
                    } 
                } 
            } 
        } 
 
        private class MyNavigation 
        { 
            private MyDriller myDriller; 
            public IMyRemoteControl RemCon; 
            public double AbsHeight { get; private set; } 
            public Vector3D MyPos { get; private set; } 
            public Vector3D MyPrevPos { get; private set; } 
            public Vector3D VelocityVector { get; private set; } 
            public Vector3D UpVelocityVector { get; private set; } 
            public Vector3D ForwVelocityVector { get; private set; } 
            public Vector3D LeftVelocityVector { get; private set; } 
            public Vector3D GravVector { get; private set; } 
            public Vector3D PlanetCenter; 
            public MatrixD DockMatrix1 { get; private set; } 
            public MatrixD DockMatrix2 { get; private set; } 
            private Vector3D ConnectorPoint = new Vector3D(0, 0, 3); 
            private Vector3D DrillPoint = new Vector3D(0, 0, 0); 
            private double FlyHeight; 
            private Vector3D BaseDockPoint; 
            public int ShaftN { get; private set; } 
            public int RockN { get; private set; } 
            public int MaxRocks { get; private set; } 
            public int Status { get; private set; } 
            public bool PullUpNeeded { get; private set; } 
 
            public MyNavigation(MyDriller mdr) 
            { 
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>(); 
                myDriller = mdr; 
                RemCon = ParentProgram.GridTerminalSystem.GetBlockWithName(myDriller.MyName + "RemCon") as IMyRemoteControl; 
                GravVector = RemCon.GetNaturalGravity(); 
                LoadFromStorage(); 
                //FindPlanetCenter();   
            } 
            public void Update() 
            { 
                MyPrevPos = MyPos; 
                MyPos = RemCon.GetPosition(); 
                GravVector = RemCon.GetNaturalGravity(); 
                VelocityVector = (MyPos - MyPrevPos) * 60 / ParentProgram.Clock; 
                UpVelocityVector = RemCon.WorldMatrix.Up * Vector3D.Dot(VelocityVector, RemCon.WorldMatrix.Up); 
                ForwVelocityVector = RemCon.WorldMatrix.Forward * Vector3D.Dot(VelocityVector, RemCon.WorldMatrix.Forward); 
                LeftVelocityVector = RemCon.WorldMatrix.Left * Vector3D.Dot(VelocityVector, RemCon.WorldMatrix.Left); 
                AbsHeight = (MyPos - PlanetCenter).Length(); 
            } 
            public double GetVal(string Key, IMyTextPanel ScrObj) 
            { 
                string val = "0"; 
                string pattern = @"(" + Key + "):([^:^;]+);"; 
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(ScrObj.GetPublicText().Replace("\n", ""), pattern); 
                if (match.Success) 
                { 
                    val = match.Groups[2].Value; 
                } 
                return Convert.ToDouble(val); 
            } 
            public int GetValInt(string Key, IMyTextPanel ScrObj) 
            { 
                string val = "0"; 
                string pattern = @"(" + Key + "):([^:^;]+);"; 
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(ScrObj.GetPublicText().Replace("\n", ""), pattern); 
                if (match.Success) 
                { 
                    val = match.Groups[2].Value; 
                } 
                return Convert.ToInt32(val); 
            } 
            public Vector3D GetRockByNum(int Key, IMyTextPanel ScrObj) 
            { 
                string pattern = @"\n(" + Key.ToString() + ");([^;^;]+);([^;^;]+);([^;^;]+);"; 
                Vector3D RockPos = new Vector3D(0, 0, 0); 
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(ScrObj.GetPublicText(), pattern); 
                if (match.Success) 
                { 
                    RockPos = new Vector3D(Convert.ToDouble(match.Groups[2].Value), Convert.ToDouble(match.Groups[3].Value), Convert.ToDouble(match.Groups[4].Value)); 
                } 
                return RockPos; 
            } 
            public void FindPlanetCenter() 
            { 
                if ((RemCon as IMyShipController).TryGetPlanetPosition(out PlanetCenter)) 
                { 
                    myDriller.TextOutput("TP_Status", "Calibration: \n Planet Center: \n X: " + PlanetCenter.GetDim(0).ToString() + "\n Y: " + PlanetCenter.GetDim(1).ToString() + "\n Z: " + PlanetCenter.GetDim(2).ToString() + "\n"); 
                    SaveToStorage(); 
                } 
            } 
            public void SetFlyHeight() 
            { 
                FlyHeight = (RemCon.GetPosition() - PlanetCenter).Length(); 
                BaseDockPoint = new Vector3D(0, 0, -300); 
                SaveToStorage(); 
            } 
            public void DisplayHeight() 
            { 
                myDriller.TextOutput("TP_Status", "Calibration: \n Planet Center: \n X: " + PlanetCenter.GetDim(0).ToString() + "\n Y: " + PlanetCenter.GetDim(1).ToString() + "\n Z: " + PlanetCenter.GetDim(2).ToString() + "\n" + Math.Round((RemCon.GetPosition() - PlanetCenter).Length(), 2).ToString() + "\n"); 
            } 
            public void LoadFromStorage() 
            { 
                IMyTextPanel ScrObj = ParentProgram.GridTerminalSystem.GetBlockWithName(myDriller.MyName + "TP_Mem") as IMyTextPanel; 
                myDriller.CurrentStatus = GetValInt("Status", ScrObj); 
                myDriller.Command = GetValInt("Command", ScrObj); 
                FlyHeight = GetVal("FlyHeight", ScrObj); 
                ShaftN = GetValInt("ShaftN", ScrObj); 
                RockN = GetValInt("RockN", ScrObj); 
                MaxRocks = GetValInt("MaxRocks", ScrObj); 
                myDriller.Paused = GetValInt("Paused", ScrObj) == 1; 
                myDriller.GoHome = GetValInt("GoHome", ScrObj) == 1; 
                myDriller.StayHome = GetValInt("StayHome", ScrObj) == 1; 
                myDriller.EmergencyReturn = GetValInt("EmergencyReturn", ScrObj) == 1; 
 
                DockMatrix1 = new MatrixD(GetVal("MC11", ScrObj), GetVal("MC12", ScrObj), GetVal("MC13", ScrObj), GetVal("MC14", ScrObj), 
                GetVal("MC21", ScrObj), GetVal("MC22", ScrObj), GetVal("MC23", ScrObj), GetVal("MC24", ScrObj), 
                GetVal("MC31", ScrObj), GetVal("MC32", ScrObj), GetVal("MC33", ScrObj), GetVal("MC34", ScrObj), 
                GetVal("MC41", ScrObj), GetVal("MC42", ScrObj), GetVal("MC43", ScrObj), GetVal("MC44", ScrObj)); 
                DockMatrix2 = new MatrixD(GetVal("MD11", ScrObj), GetVal("MD12", ScrObj), GetVal("MD13", ScrObj), GetVal("MD14", ScrObj), 
                GetVal("MD21", ScrObj), GetVal("MD22", ScrObj), GetVal("MD23", ScrObj), GetVal("MD24", ScrObj), 
                GetVal("MD31", ScrObj), GetVal("MD32", ScrObj), GetVal("MD33", ScrObj), GetVal("MD34", ScrObj), 
                GetVal("MD41", ScrObj), GetVal("MD42", ScrObj), GetVal("MD43", ScrObj), GetVal("MD44", ScrObj)); 
                PlanetCenter = new Vector3D(GetVal("PX", ScrObj), GetVal("PY", ScrObj), GetVal("PZ", ScrObj)); 
 
                BaseDockPoint = new Vector3D(0, 0, -200); 
            } 
            public void SaveToStorage() 
            { 
                IMyTextPanel ScrObj = ParentProgram.GridTerminalSystem.GetBlockWithName(myDriller.MyName + "TP_Mem") as IMyTextPanel; 
                string NavData = ""; 
                NavData += "Command:" + myDriller.Command.ToString() + ";\n"; 
                NavData += "Status:" + myDriller.CurrentStatus.ToString() + ";\n"; 
                NavData += "Paused:" + (myDriller.Paused ? 1 : 0).ToString() + ";\n"; 
                NavData += "FlyHeight:" + Math.Round(FlyHeight, 0).ToString() + ";\n"; 
                NavData += "ShaftN:" + ShaftN.ToString() + ";\n"; 
                NavData += "RockN:" + RockN.ToString() + ";\n"; 
                NavData += "MaxRocks:" + MaxRocks.ToString() + ";\n"; 
                NavData += "EmergencyReturn:" + (myDriller.EmergencyReturn ? 1 : 0).ToString() + ";\n"; 
                NavData += "GoHome:" + (myDriller.GoHome ? 1 : 0).ToString() + ";\n"; 
                NavData += "StayHome:" + (myDriller.StayHome ? 1 : 0).ToString() + ";\n"; 
                NavData += DockMatrix1.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "MC"); 
                NavData += DockMatrix2.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("M", "MD"); 
                NavData += PlanetCenter.ToString().Replace("}", "").Replace("{", "").Replace(" ", " ").Replace(" ", ";\n").Replace("X", "PX").Replace("Y", "PY").Replace("Z", "PZ") + ";\n"; 
 
                ScrObj.ShowTextureOnScreen(); 
                ScrObj.WritePublicText(NavData); 
                ScrObj.ShowPublicTextOnScreen(); 
                ScrObj.GetActionWithName("OnOff_On").Apply(ScrObj); 
            } 
            public MatrixD GetTransMatrixFromMyPos() 
            { 
                MatrixD mRot; 
                Vector3D V3Dcenter = RemCon.GetPosition(); 
                Vector3D V3Dfow = RemCon.WorldMatrix.Forward; 
                Vector3D V3Dup = RemCon.WorldMatrix.Up; 
                Vector3D V3Dleft = RemCon.WorldMatrix.Left; 
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1); 
                mRot = MatrixD.Invert(mRot); 
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot; 
            } 
            public MatrixD GetNormTransMatrixFromMyPos() 
            { 
                MatrixD mRot; 
                Vector3D V3Dcenter = RemCon.GetPosition(); 
                Vector3D V3Dup = RemCon.WorldMatrix.Up; 
                if (GravVector.LengthSquared() > 0.2f) 
                    V3Dup = -Vector3D.Normalize(GravVector); 
 
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.Reject(RemCon.WorldMatrix.Left, V3Dup)); 
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup)); 
 
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1); 
                mRot = MatrixD.Invert(mRot); 
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot; 
            } 
            public MatrixD GetNormTransMatrixFromPoint(Vector3D Point) 
            { 
                MatrixD mRot; 
                Vector3D V3Dcenter = Point; 
                Vector3D V3Dup = Vector3D.Normalize(V3Dcenter - PlanetCenter); 
                Vector3D V3Dleft = Vector3D.Normalize(Vector3D.CalculatePerpendicularVector(V3Dup)); 
                Vector3D V3Dfow = Vector3D.Normalize(Vector3D.Cross(V3Dleft, V3Dup)); 
 
                mRot = new MatrixD(V3Dleft.GetDim(0), V3Dleft.GetDim(1), V3Dleft.GetDim(2), 0, V3Dup.GetDim(0), V3Dup.GetDim(1), V3Dup.GetDim(2), 0, V3Dfow.GetDim(0), V3Dfow.GetDim(1), V3Dfow.GetDim(2), 0, 0, 0, 0, 1); 
                mRot = MatrixD.Invert(mRot); 
                return new MatrixD(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -V3Dcenter.GetDim(0), -V3Dcenter.GetDim(1), -V3Dcenter.GetDim(2), 1) * mRot; 
            } 
 
            public void SetDockMatrix1() 
            { 
                DockMatrix1 = GetNormTransMatrixFromMyPos(); 
            } 
 
            public void SetDockMatrix2() 
            { 
                DockMatrix2 = GetNormTransMatrixFromMyPos(); 
            } 
 
            public Vector3D GetNavAngles(Vector3D Target, MatrixD InvMatrix, double shiftX = 0, double shiftZ = 0) 
            { 
                double TargetPitch = 0, TargetRoll = 0, TargetYaw = 0; 
                Vector3D V3Dcenter = RemCon.GetPosition(); 
                Vector3D V3Dfow = RemCon.WorldMatrix.Forward + V3Dcenter; 
                Vector3D V3Dup = RemCon.WorldMatrix.Up + V3Dcenter; 
                Vector3D V3Dleft = RemCon.WorldMatrix.Left + V3Dcenter; 
 
                V3Dcenter = Vector3D.Transform(V3Dcenter, InvMatrix); 
                V3Dfow = (Vector3D.Transform(V3Dfow, InvMatrix)) - V3Dcenter; 
                V3Dup = (Vector3D.Transform(V3Dup, InvMatrix)) - V3Dcenter; 
                V3Dleft = (Vector3D.Transform(V3Dleft, InvMatrix)) - V3Dcenter; 
 
                Vector3D GravNorm = Vector3D.Normalize(new Vector3D(-shiftX, -1, -shiftZ)); 
                Vector3D TargetNorm = Vector3D.Normalize(Target - V3Dcenter); 
 
                if (GravVector.LengthSquared() > 0.2f) 
                { 
                    GravNorm = Vector3D.Normalize(GravVector); 
                    GravNorm = Vector3D.Normalize(Vector3D.Transform(GravNorm + RemCon.GetPosition(), InvMatrix) - V3Dcenter - new Vector3D(shiftX, 0, shiftZ)); 
                } 
 
                if ((GravVector.LengthSquared() < 0.2f) && ((myDriller.CurrentStatus == Commands.ToBase1) || (myDriller.CurrentStatus == Commands.ToBase2))) 
                { 
                    TargetPitch = Vector3D.Dot(V3Dup, Vector3D.Normalize(Vector3D.Reject(TargetNorm, V3Dleft))); 
                    TargetPitch = -Math.Acos(TargetPitch) + Math.PI / 2; 
                    TargetYaw = Vector3D.Dot(V3Dleft, Vector3D.Normalize(Vector3D.Reject(TargetNorm, V3Dup))); 
                    TargetYaw = Math.Acos(TargetYaw) - Math.PI / 2; 
                    TargetRoll = 0; 
                } 
                else 
                { 
                    TargetNorm = Vector3D.Normalize(Vector3D.Reject(Target - V3Dcenter, GravNorm)); 
                    TargetPitch = Vector3D.Dot(V3Dfow, Vector3D.Normalize(Vector3D.Reject(-GravNorm, V3Dleft))); 
                    TargetPitch = Math.Acos(TargetPitch) - Math.PI / 2; 
 
                    TargetRoll = Vector3D.Dot(V3Dleft, Vector3D.Reject(-GravNorm, V3Dfow)); 
                    TargetRoll = Math.Acos(TargetRoll) - Math.PI / 2; 
 
                    TargetYaw = Math.Acos(Vector3D.Dot(V3Dfow, TargetNorm)); 
                    if ((V3Dleft - TargetNorm).Length() < Math.Sqrt(2)) 
                        TargetYaw = -TargetYaw; 
                } 
                if (double.IsNaN(TargetYaw)) TargetYaw = 0; 
                if (double.IsNaN(TargetPitch)) TargetPitch = 0; 
                if (double.IsNaN(TargetRoll)) TargetRoll = 0; 
                return new Vector3D(TargetYaw, TargetPitch, TargetRoll); 
            } 
            public bool Dock(MatrixD InvMatrix) 
            { 
                bool Complete = false; 
                float MaxUSpeed, MaxLSpeed, MaxFSpeed; 
                Vector3D MyPosCon = Vector3D.Transform(MyPos, InvMatrix); 
                Vector3D gyrAng = GetNavAngles(ConnectorPoint, InvMatrix); 
                float Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, InvMatrix)))).Length() + ConnectorPoint.Length()); 
 
                MaxLSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(0)) * myDriller.thrustBlock.XMaxA) / 2; 
                MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(MyPosCon.GetDim(1)) * myDriller.thrustBlock.YMaxA) / 2; 
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * myDriller.thrustBlock.ZMaxA) / 2; 
                if (Distance < 15) 
                    MaxFSpeed = MaxFSpeed / 5; 
                if (Math.Abs(MyPosCon.GetDim(1)) < 1) 
                    MaxUSpeed = 0.1f; 
                myDriller.gyroBlock.SetOverride(true, gyrAng * ParentProgram.GyroMult, 1); 
                if (LeftVelocityVector.Length() < MaxLSpeed) 
                    myDriller.thrustBlock.SetOverrideAccel("R", (float)(MyPosCon.GetDim(0) * ParentProgram.AlignAccelMult)); 
                else 
                { 
                    myDriller.thrustBlock.SetOverridePercent("R", 0); 
                    myDriller.thrustBlock.SetOverridePercent("L", 0); 
                } 
                float UpAccel = -(float)(MyPosCon.GetDim(1) * ParentProgram.AlignAccelMult); 
                float minUpAccel = 0.3f; 
                if ((UpAccel < 0) && (UpAccel > -minUpAccel)) 
                    UpAccel = -minUpAccel; 
                if ((UpAccel > 0) && (UpAccel < minUpAccel)) 
                    UpAccel = minUpAccel; 
 
                if (UpVelocityVector.Length() < MaxUSpeed) 
                    myDriller.thrustBlock.SetOverrideAccel("U", UpAccel); 
                else 
                { 
                    myDriller.thrustBlock.SetOverridePercent("U", 0); 
                } 
                if (((Distance > 100) || ((Math.Abs(MyPosCon.GetDim(0)) < (Distance / 10 + 0.2f)) && (Math.Abs(MyPosCon.GetDim(1)) < (Distance / 10 + 0.2f)))) && (ForwVelocityVector.Length() < MaxFSpeed)) 
                { 
                    myDriller.thrustBlock.SetOverrideAccel("F", (float)(Distance * ParentProgram.AlignAccelMult)); 
                    myDriller.thrustBlock.SetOverridePercent("B", 0); 
                } 
                else 
                { 
                    myDriller.thrustBlock.SetOverridePercent("F", 0); 
                    myDriller.thrustBlock.SetOverridePercent("B", 0); 
                } 
                if (Distance < 6) 
                { 
                    myDriller.connectorBlock.Turn("On"); 
                    if (myDriller.connectorBlock.Connector.Status == MyShipConnectorStatus.Connectable) 
                        Complete = myDriller.connectorBlock.Lock(); 
                } 
                string strStatus = " STATUS\n"; 
                strStatus += "Task: Docking \n"; 
                strStatus += "Ship Mass: " + myDriller.thrustBlock.TotalMass.ToString() + "\n"; 
                strStatus += "Battery charge: " + Math.Round(myDriller.batteryBlock.StoredPower * 100 / myDriller.batteryBlock.MaxPower, 2).ToString() + " % \n"; 
                strStatus += "XY shifts: " + Math.Round(MyPosCon.GetDim(0), 2).ToString() + " / " + Math.Round(MyPosCon.GetDim(1), 2).ToString() + "\n"; 
                strStatus += "Distance: " + Math.Round(Distance).ToString() + "\n"; 
                strStatus += "Connector: " + (Complete ? "Locked" : "Unlocked") + "\n"; 
                myDriller.TextOutput("TP_Status", strStatus); 
                //return false;             
                return Complete; 
            } 
 
            public bool UnloadAndRecharge(bool Load) 
            { 
                bool Complete = true; 
                if (Load) 
                { 
                    myDriller.cargoBlock.Load(); 
                    //Complete = false; 
                } 
                else if (myDriller.cargoBlock.CurrentMass > 0) 
                { 
                    myDriller.cargoBlock.UnLoad(); 
                    Complete = false; 
                } 
 
                if ((myDriller.batteryBlock.MaxPower - myDriller.batteryBlock.StoredPower) > 0.5f) 
                { 
                    myDriller.batteryBlock.Recharge(true); 
                    Complete = false; 
                } 
                else myDriller.batteryBlock.Recharge(false); 
                if (!Complete) 
                { 
                    if (myDriller.connectorBlock.Locked) 
                    { 
                        myDriller.thrustBlock.Turn("Off"); 
                    } 
                } 
                else 
                { 
                    myDriller.thrustBlock.Turn("On"); 
                    myDriller.thrustBlock.SetOverridePercent("U", 0); 
                    myDriller.thrustBlock.SetOverridePercent("R", 0); 
                    myDriller.thrustBlock.SetOverridePercent("L", 0); 
                    myDriller.thrustBlock.SetOverridePercent("F", 0); 
                } 
                string strStatus = " STATUS\n"; 
                strStatus += "Task: Unload & Recharge\n"; 
                strStatus += "Ship Mass: " + myDriller.thrustBlock.TotalMass.ToString() + "\n"; 
                strStatus += "Battery charge: " + Math.Round(myDriller.batteryBlock.StoredPower * 100 / myDriller.batteryBlock.MaxPower, 2).ToString() + " % \n"; 
                strStatus += "Connector: " + (myDriller.connectorBlock.Locked ? "Locked" : "Unlocked") + "\n"; 
                strStatus += "\n !WARNING \n"; 
                strStatus += "Do not disconnect!\n"; 
                myDriller.TextOutput("TP_Status", strStatus); 
                //myDriller.TextOutput("TP_Status", Complete.ToString());             
                return Complete; 
            } 
 
            public bool UnDock(MatrixD InvMatrix) 
            { 
                bool Complete = false; 
                float Distance = 0; 
                if (myDriller.connectorBlock.UnLock()) 
                { 
                    Vector3D MyPosCon = Vector3D.Transform(MyPos, InvMatrix); 
                    Vector3D gyrAng = GetNavAngles(ConnectorPoint, InvMatrix); 
                    Distance = (float)((Vector3D.Reject(MyPosCon, Vector3D.Normalize(Vector3D.Transform(PlanetCenter, InvMatrix)))).Length() + ConnectorPoint.Length()); 
                    myDriller.gyroBlock.SetOverride(true, gyrAng * ParentProgram.GyroMult, 1); 
                    myDriller.thrustBlock.SetOverridePercent("U", 0); 
                    myDriller.thrustBlock.SetOverridePercent("R", 0); 
                    myDriller.thrustBlock.SetOverridePercent("L", 0); 
                    myDriller.thrustBlock.SetOverridePercent("F", 0); 
                    myDriller.thrustBlock.SetOverrideAccel("B", 3); 
                    if (Distance > 50) 
                    { 
                        Complete = true; 
                    } 
                } 
                string strStatus = " STATUS\n"; 
                strStatus += "Task: Undocking\n"; 
                strStatus += "Ship Mass: " + myDriller.thrustBlock.TotalMass.ToString() + "\n"; 
                strStatus += "Battery charge: " + Math.Round(myDriller.batteryBlock.StoredPower * 100 / myDriller.batteryBlock.MaxPower, 2).ToString() + " % \n"; 
                strStatus += "Connector: " + (myDriller.connectorBlock.Locked ? "Locked" : "Unlocked") + "\n"; 
                strStatus += "Distance: " + Math.Round(Distance).ToString() + "\n"; 
                myDriller.TextOutput("TP_Status", strStatus); 
                return Complete; 
            } 
            public bool ToBase(MatrixD InvMatrix) 
            { 
                myDriller.TextOutput("TP_Rock", "ToBase"); 
                bool Complete = false; 
                float MaxUSpeed = 0f, MaxFSpeed = 0f; 
                Vector3D gyrAng = GetNavAngles(BaseDockPoint, InvMatrix); 
                Vector3D MyPosCon = Vector3D.Transform(MyPos, InvMatrix); 
                double FHTemp = FlyHeight; 
                if (Vector3D.Transform(PlanetCenter, InvMatrix).Length() > FlyHeight) 
                    FHTemp += FHTemp; 
                if (RemCon.GetNaturalGravity().LengthSquared() > 0.2f) 
                    MaxUSpeed = (float)Math.Sqrt(2 * Math.Abs(FHTemp - (MyPos - PlanetCenter).Length()) * myDriller.thrustBlock.YMaxA) / 1.2f; 
                float Distance = (float)(BaseDockPoint - new Vector3D(MyPosCon.GetDim(0), 0, MyPosCon.GetDim(2))).Length(); 
                MaxFSpeed = (float)Math.Sqrt(2 * Distance * myDriller.thrustBlock.ZMaxA) / 1.2f; 
                myDriller.gyroBlock.SetOverride(true, gyrAng * ParentProgram.GyroMult, 1); 
                myDriller.thrustBlock.SetOverridePercent("R", 0); 
                myDriller.thrustBlock.SetOverridePercent("L", 0); 
 
                if (UpVelocityVector.Length() < MaxUSpeed) 
                    myDriller.thrustBlock.SetOverrideAccel("U", (float)((FHTemp - (MyPos - PlanetCenter).Length()) * ParentProgram.AlignAccelMult)); 
                else 
                { 
                    myDriller.thrustBlock.SetOverridePercent("U", 0); 
                } 
                // myDriller.TextOutput("TP1", "\n" + MaxFSpeed.ToString() + "\n" + ForwVelocityVector.Length().ToString() + "\n" + Distance.ToString());             
                if (Distance > ParentProgram.TargetSize) 
                { 
                    if (ForwVelocityVector.Length() < MaxFSpeed) 
                    { 
                        myDriller.thrustBlock.SetOverrideAccel("F", (float)(Distance * ParentProgram.AlignAccelMult)); 
                        myDriller.thrustBlock.SetOverridePercent("B", 0); 
                    } 
                    else 
                    { 
                        myDriller.thrustBlock.SetOverridePercent("F", 0); 
                        myDriller.thrustBlock.SetOverridePercent("B", 0); 
                    } 
                } 
                else 
                { 
                    Complete = true; 
                } 
                string strStatus = " STATUS\n"; 
                strStatus += "Task: To base\n"; 
                strStatus += "Battery charge: " + Math.Round(myDriller.batteryBlock.StoredPower * 100 / myDriller.batteryBlock.MaxPower, 2).ToString() + " % \n"; 
                strStatus += "Height: " + Math.Round((MyPos - PlanetCenter).Length()).ToString() + " / " + Math.Round(FlyHeight).ToString() + "\n"; 
                strStatus += "Distance: " + Math.Round(Distance).ToString() + "\n"; 
                myDriller.TextOutput("TP_Status", strStatus); 
                return Complete; 
            } 
 
 
        } 
        private class MyCargo 
        { 
            public int MaxVolume { get; private set; } 
            public int CriticalMass { get; private set; } 
            public int CurrentVolume { get; private set; } 
            public int CurrentMass { get; private set; } 
            public int CurrentOreMass { get; private set; } 
            public int FeAmount { get; private set; } 
            public int CbAmount { get; private set; } 
            public int NiAmount { get; private set; } 
            public int MgAmount { get; private set; } 
            public int AuAmount { get; private set; } 
            public int AgAmount { get; private set; } 
            public int PtAmount { get; private set; } 
            public int SiAmount { get; private set; } 
            public int UAmount { get; private set; } 
            public int StoneAmount { get; private set; } 
            public int IceAmount { get; private set; } 
            public bool StoneDumpNeeded { get; private set; } 
            public bool CriticalMassReached { get; private set; } 
 
            private MyDriller myDriller; 
            private List<IMyTerminalBlock> CargoGroup = new List<IMyTerminalBlock>(); 
 
            public MyCargo(MyDriller mdr) 
            { 
                myDriller = mdr; 
                CriticalMass = (int)(ParentProgram.CriticalMass); 
                List<IMyTerminalBlock> TempGroup = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(TempGroup); 
                for (int i = 0; i < TempGroup.Count; i++) 
                { 
                    if (TempGroup[i].CustomName.StartsWith(myDriller.MyName)) 
                    { 
                        IMyTerminalBlock CargoOwner = TempGroup[i] as IMyTerminalBlock; 
                        if ((CargoOwner != null)) 
                            if ((CargoOwner is IMyShipDrill) || (CargoOwner is IMyCargoContainer) || (CargoOwner is IMyShipConnector)) 
                            { 
                                MaxVolume += (int)CargoOwner.GetInventory(0).MaxVolume; 
                                CurrentVolume += (int)(CargoOwner.GetInventory(0).CurrentVolume * 1000); 
                                CurrentMass += (int)CargoOwner.GetInventory(0).CurrentMass; 
                                CargoGroup.Add(CargoOwner); 
                            } 
                    } 
                } 
            } 
 
            private void OutputCargoList() 
            { 
                string Output = " CARGO: " + ((int)(myDriller.thrustBlock.TotalMass * 100 / CriticalMass)).ToString() + "%"; 
                Output += "\n CurrentMass: " + CurrentMass.ToString(); 
                //Output+="\n Cargo Mass: "+Math.Round(CargoMass,1);             
                if (FeAmount > 0) { Output += "\n Fe: " + FeAmount; } 
                if (CbAmount > 0) { Output += "\n Cb: " + CbAmount; } 
                if (NiAmount > 0) { Output += "\n Ni: " + NiAmount; } 
                if (MgAmount > 0) { Output += "\n Mg: " + MgAmount; } 
                if (AuAmount > 0) { Output += "\n Au: " + AuAmount; } 
                if (AgAmount > 0) { Output += "\n Ag: " + AgAmount; } 
                if (PtAmount > 0) { Output += "\n Pt: " + PtAmount; } 
                if (SiAmount > 0) { Output += "\n Si: " + SiAmount; } 
                if (UAmount > 0) { Output += "\n U: " + UAmount; } 
                if (IceAmount > 0) { Output += "\n Ice: " + IceAmount; } 
                if (StoneAmount > 0) { Output += "\n Stone: " + StoneAmount; } 
                Output += "\n Crit Mass:" + CriticalMassReached.ToString(); 
                Output += "\n Dump Stone:" + StoneDumpNeeded.ToString(); 
                myDriller.TextOutput("TP_Cargo", Output); 
            } 
 
            public void UnLoad() 
            { 
                var BaseCargo = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName("BaseCargo", BaseCargo); 
 
                for (int ii = 0; ii < BaseCargo.Count; ii++) 
                { 
                    var Destination = BaseCargo[ii].GetInventory(0); 
                    for (int i = 0; i < CargoGroup.Count; i++) 
                    { 
                        var containerInvOwner = CargoGroup[i]; 
                        var containerInv = containerInvOwner.GetInventory(0); 
                        var containerItems = containerInv.GetItems(); 
                        for (int j = 0; j < containerItems.Count; j++) 
                        { 
                            containerInv.TransferItemTo(Destination, 0, null, true, null); 
                        } 
                    } 
                } 
                Update(); 
            } 
 
            public void Load() 
            { 
                var BaseCargo = new List<IMyTerminalBlock>(); 
                ParentProgram.GridTerminalSystem.SearchBlocksOfName("BaseCargo", BaseCargo); 
 
                for (int ii = 0; ii < CargoGroup.Count; ii++) 
                { 
                    var Destination = CargoGroup[ii].GetInventory(0); 
                    for (int i = 0; i < BaseCargo.Count; i++) 
                    { 
                        var containerInvOwner = BaseCargo[i]; 
                        var containerInv = containerInvOwner.GetInventory(0); 
                        var containerItems = containerInv.GetItems(); 
                        for (int j = 0; j < containerItems.Count; j++) 
                        { 
                            containerInv.TransferItemTo(Destination, 0, null, true, null); 
                        } 
                         
 
                    /*    if (myDriller.navBlock.RemCon.CalculateShipMass().PhysicalMass > ParentProgram.CriticalMass) 
                        { 
                            i = BaseCargo.Count; 
                            ii = CargoGroup.Count; 
                        }*/ 
                    } 
                } 
                Update(); 
            } 
 
            public void Update() 
            { 
                CurrentVolume = 0; 
                CurrentMass = 0; 
                FeAmount = 0; 
                CbAmount = 0; 
                NiAmount = 0; 
                MgAmount = 0; 
                AuAmount = 0; 
                AgAmount = 0; 
                PtAmount = 0; 
                SiAmount = 0; 
                UAmount = 0; 
                StoneAmount = 0; 
                IceAmount = 0; 
                for (int i = 0; i < CargoGroup.Count; i++) 
                { 
                    IMyTerminalBlock CargoOwner = CargoGroup[i]; 
                    if (CargoOwner != null) 
                    { 
                        CurrentVolume += (int)CargoOwner.GetInventory(0).CurrentVolume; 
                        CurrentMass += (int)CargoOwner.GetInventory(0).CurrentMass; 
 
                        var crateItems = CargoOwner.GetInventory(0).GetItems(); 
 
                        for (int j = crateItems.Count - 1; j >= 0; j--) 
                        { 
                            if (crateItems[j].Content.SubtypeName == "Iron") 
                                FeAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Cobalt") 
                                CbAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Nickel") 
                                NiAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Magnesium") 
                                MgAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Gold") 
                                AuAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Silver") 
                                AgAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Platinum") 
                                PtAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Silicon") 
                                SiAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Uranium") 
                                UAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Stone") 
                                StoneAmount += (int)crateItems[j].Amount; 
                            else if (crateItems[j].Content.SubtypeName == "Ice") 
                                IceAmount += (int)crateItems[j].Amount; 
                        } 
                    } 
                } 
                if (myDriller.thrustBlock.TotalMass > CriticalMass) 
                { 
                    CriticalMassReached = true; 
                } 
                else 
                { 
                    CriticalMassReached = false; 
                } 
                OutputCargoList(); 
            } 
        } 
 
        private class MyBatteries 
        { 
            public List<IMyTerminalBlock> BatteryGroup = new List<IMyTerminalBlock>(); 
            public bool LowPower { get; private set; } 
            public bool IsCharging { get; private set; } 
            public float MaxPower { get; private set; } 
            public float StoredPower { get; private set; } 
            public float InitialPower { get; private set; } 
            public float MinPower { get; private set; } 
 
            private MyDriller myDriller; 
            public MyBatteries(MyDriller mdr) 
            { 
                myDriller = mdr; 
                List<IMyTerminalBlock> TempGroup = new List<IMyTerminalBlock>(); 
                BatteryGroup.Clear(); 
                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(TempGroup); 
                MaxPower = 0; 
                MinPower = ParentProgram.ReturnOnCharge; 
                for (int i = 0; i < TempGroup.Count; i++) 
                { 
                    if (TempGroup[i].CustomName.StartsWith(myDriller.MyName)) 
                    { 
                        IMyBatteryBlock battery = TempGroup[i] as IMyBatteryBlock; 
                        if (battery != null) 
                        { 
                            BatteryGroup.Add(battery); 
                            MaxPower += battery.MaxStoredPower; 
                        } 
                    } 
                } 
            } 
            public void Recharge(bool bRecharge) 
            { 
                for (int i = 0; i < BatteryGroup.Count; i++) 
                { 
                    IMyBatteryBlock battery = BatteryGroup[i] as IMyBatteryBlock; 
                    if (battery != null) 
                        battery.SetValueBool("Recharge", bRecharge); 
                } 
                IsCharging = bRecharge; 
            } 
            public void Update() 
            { 
                StoredPower = 0; 
                for (int i = 0; i < BatteryGroup.Count; i++) 
                { 
                    IMyBatteryBlock battery = BatteryGroup[i] as IMyBatteryBlock; 
                    if (battery != null) 
                    { 
                        StoredPower += battery.CurrentStoredPower; 
                    } 
                } 
                LowPower = (StoredPower / MaxPower) < MinPower; 
            } 
        } 
 
        public class MyConnector 
        { 
            private MyDriller myDriller; 
            public IMyShipConnector Connector { get; private set; } 
            public IMyShipConnector BaseConnector { get; private set; } 
            public bool On { get; private set; } 
            public bool Locked { get; private set; } 
            public bool ReadyToLock { get; private set; } 
 
            List<IMyTerminalBlock> ConnectorGroup = new List<IMyTerminalBlock>(); 
 
            public MyConnector(MyDriller mdr) 
            { 
                myDriller = mdr; 
                Connector = ParentProgram.GridTerminalSystem.GetBlockWithName(myDriller.MyName + "Connector") as IMyShipConnector; 
                Locked = (Connector.Status == MyShipConnectorStatus.Connected); 
            } 
 
            public void Update() 
            { 
                if (Connector == null) 
                    Connector = ParentProgram.GridTerminalSystem.GetBlockWithName(myDriller.MyName + "Connector") as IMyShipConnector; 
                else 
                { 
                    Locked = (Connector.Status == MyShipConnectorStatus.Connected); 
                    //if (Locked)             
                    //BaseConnector = Connector.OtherConnector;             
                } 
            } 
            public void Turn(string OnOff) 
            { 
                if (Connector != null) 
                { 
                    Connector.GetActionWithName("OnOff_" + OnOff).Apply(Connector); 
                    if (OnOff == "On") 
                        On = true; 
                    else 
                        On = false; 
                } 
                else 
                { 
                    //Connector crashed event!!!             
                } 
            } 
 
            public bool Lock() 
            { 
                Connector.GetActionWithName("OnOff_On").Apply(Connector); 
                Connector.GetActionWithName("Lock").Apply(Connector); 
                Locked = (Connector.Status == MyShipConnectorStatus.Connected); 
                return Locked; 
            } 
 
            public bool UnLock() 
            { 
                if (Connector.Status == MyShipConnectorStatus.Connected) 
                    BaseConnector = Connector.OtherConnector; 
                Connector.GetActionWithName("Unlock").Apply(Connector); 
                Connector.GetActionWithName("OnOff_Off").Apply(Connector); 
                Locked = (Connector.Status == MyShipConnectorStatus.Connected); 
                return !Locked; 
            } 
        } 
 
    }