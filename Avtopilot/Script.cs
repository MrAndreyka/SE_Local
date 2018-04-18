 Follower Follower1;

        float koeffA = 5.0f;
        float koeffV = 5.0f;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main(string arg, UpdateType UT)
        {
            if (UT == UpdateType.Update100)
            {
                Follower1 = new Follower(this, koeffA, koeffV);
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            }
            else if (UT == UpdateType.Update1)
            {
                //будем перемещаться к этой точке
                //GPS:Pennywise #1:-13010.66:50598.2:32509.12:
                //будем держать прицел на эту точку:
                //GPS:Pennywise #2:-12976.41:50487.77:32242.19:
                //Follower1.GoToPos(new Vector3D(-13010.66, 50598.2, 32509.12), new Vector3D(-12976.41, 50487.77, 32242.19));
                Follower1.FollowMe();
            }

        }

        public class Follower
        {
            string OwnerName;
            IMyShipController RemCon;
            static Program ParentProgram;
            MyThrusters myThr;
            MyGyros myGyros;
            MySensors mySensors;
            MyWeapons myWeapons;
            float kV;
            float kA;

            public Follower(Program parenProg, float koeffA, float koeffV)
            {
                ParentProgram = parenProg;
                kV = koeffV;
                kA = koeffA;
                InitMainBlocks();
                InitSubSystems();
                OwnerName = parenProg.Me.CustomData;
            }

            private void InitMainBlocks()
            {
                RemCon = new Selection(null).FindBlock<IMyShipController>(ParentProgram.GridTerminalSystem);
                if (RemCon == null) ParentProgram.Echo("Нет блока упаравления");
            }

            private void InitSubSystems()
            {
                myThr = new MyThrusters(this);
                myGyros = new MyGyros(this, 3);
                mySensors = new MySensors(this);
                myWeapons = new MyWeapons(this);
            }

            public void TestDrive(Vector3D Thr)
            {
                myThr.SetThrA(Thr);
            }

            public void TestHover()
            {
                Vector3D GravAccel = RemCon.GetNaturalGravity();
                //MatrixD MyMatrix = MatrixD.Invert(RemCon.WorldMatrix.GetOrientation());
                //myThr.SetThrA(Vector3D.Transform(-GravAccel, MyMatrix));

                MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
                myThr.SetThrA(VectorTransform(-GravAccel, MyMatrix));
            }


            public void FollowMe()
            {
                mySensors.UpdateSensors();
                if (mySensors.OwnerDetected)
                {
                    GoToPos(mySensors.DetectedOwner.Position - RemCon.GetNaturalGravity());
                }
                else
                {
                    //Здесь что-то надо делать, если потерян контакт с хозяином
                }
                if (mySensors.EnemyDetected)
                {
                    Fire(mySensors.DetectedEnemy.Position);
                }
                else
                {
                    //Здесь не обнаружен враг
                }
            }

            public void Fire(Vector3D Pos)
            {
                MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
                if (myGyros.LookAtPoint(VectorTransform(mySensors.DetectedEnemy.Position - RemCon.GetPosition(), MyMatrix)) < 0.1)
                {
                    myWeapons.Fire();
                }


            }

            public void GoToPos(Vector3D Pos)
            {
                Vector3D GravAccel = RemCon.GetNaturalGravity();
                MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
                //Расчитать расстояние до цели
                Vector3D TargetVector = Pos - RemCon.GetPosition();
                Vector3D TargetVectorNorm = Vector3D.Normalize(TargetVector);
                //Расчитать желаемую скорость
                Vector3D DesiredVelocity = TargetVector * Math.Sqrt(2 * kV / TargetVector.Length());
                Vector3D VelocityDelta = DesiredVelocity - RemCon.GetShipVelocities().LinearVelocity;
                //Расчитать желаемое ускорение
                Vector3D DesiredAcceleration = VelocityDelta * kA;
                //Передаем желаемое ускорение с учетом гравитации движкам
                myThr.SetThrA(VectorTransform(DesiredAcceleration - GravAccel, MyMatrix));
            }

            public Vector3D VectorTransform(Vector3D Vec, MatrixD Orientation)
            {
                return new Vector3D(Vec.Dot(Orientation.Right), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Backward));
            }

            private class MyWeapons:List<IMySmallGatlingGun> 
            {
                Follower myBot;
                public MyWeapons(Follower mbt)
                {
                    myBot = mbt;
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


            private class MySensors: List<IMySensorBlock>
            {
                Follower myBot;
                public List<MyDetectedEntityInfo> DetectedEntities;
                public MyDetectedEntityInfo DetectedOwner;
                public bool OwnerDetected;
                public MyDetectedEntityInfo DetectedEnemy;
                public bool EnemyDetected;


                public MySensors(Follower mbt)
                {
                    myBot = mbt;
                    InitMainBlocks();
                }

                private void InitMainBlocks()
                {
                    DetectedEntities = new List<MyDetectedEntityInfo>();
                    ParentProgram.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(this);
                }

                public void UpdateSensors()
                {
                    OwnerDetected = false;
                    EnemyDetected = false;
                    foreach (IMySensorBlock sensor in this)
                    {
                        sensor.DetectedEntities(DetectedEntities);
                        ParentProgram.Echo("Entities " + DetectedEntities.Count);
                        foreach (MyDetectedEntityInfo detEnt in DetectedEntities)
                        {
                            ParentProgram.Echo(detEnt.Name);
                            if (!OwnerDetected && detEnt.Name == myBot.OwnerName)
                            {
                                ParentProgram.Echo(detEnt.Position.ToString());
                                DetectedOwner = detEnt;
                                OwnerDetected = true;
                            }

                            if (!EnemyDetected && detEnt.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                            {
                                DetectedEnemy = detEnt;
                                EnemyDetected = true;
                            }

                        }
                    }
                }

            }

            private class MyGyros:List<IMyGyro>
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

            private class MyThrusters:List<IMyThrust>
            {
                Follower myBot;
                List<IMyThrust> UpThrusters;
                List<IMyThrust> DownThrusters;
                List<IMyThrust> LeftThrusters;
                List<IMyThrust> RightThrusters;
                List<IMyThrust> ForwardThrusters;
                List<IMyThrust> BackwardThrusters;

                double UpThrMax;
                double DownThrMax;
                double LeftThrMax;
                double RightThrMax;
                double ForwardThrMax;
                double BackwardThrMax;


                //переменные подсистемы двигателей
                public MyThrusters(Follower mbt)
                {
                    myBot = mbt;
                    InitMainBlocks();
                }

                private void InitMainBlocks()
                {
                    Matrix ThrLocM = new Matrix();
                    Matrix MainLocM = new Matrix();
                    myBot.RemCon.Orientation.GetMatrix(out MainLocM);

                    UpThrusters = new List<IMyThrust>();
                    DownThrusters = new List<IMyThrust>();
                    LeftThrusters = new List<IMyThrust>();
                    RightThrusters = new List<IMyThrust>();
                    ForwardThrusters = new List<IMyThrust>();
                    BackwardThrusters = new List<IMyThrust>();
                    UpThrMax = 0;
                    DownThrMax = 0;
                    LeftThrMax = 0;
                    RightThrMax = 0;
                    ForwardThrMax = 0;
                    BackwardThrMax = 0;

                    ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyThrust>(this);

                    for (int i = 0; i < Count; i++)
                    {
                        IMyThrust Thrust = this[i];
                        Thrust.Orientation.GetMatrix(out ThrLocM);
                        //Y
                        if (ThrLocM.Backward == MainLocM.Up)
                        {
                            UpThrusters.Add(Thrust);
                            UpThrMax += Thrust.MaxEffectiveThrust;
                        }
                        else if (ThrLocM.Backward == MainLocM.Down)
                        {
                            DownThrusters.Add(Thrust);
                            DownThrMax += Thrust.MaxEffectiveThrust;
                        }
                        //X
                        else if (ThrLocM.Backward == MainLocM.Left)
                        {
                            LeftThrusters.Add(Thrust);
                            LeftThrMax += Thrust.MaxEffectiveThrust;
                        }
                        else if (ThrLocM.Backward == MainLocM.Right)
                        {
                            RightThrusters.Add(Thrust);
                            RightThrMax += Thrust.MaxEffectiveThrust;
                        }
                        //Z
                        else if (ThrLocM.Backward == MainLocM.Forward)
                        {
                            ForwardThrusters.Add(Thrust);
                            ForwardThrMax += Thrust.MaxEffectiveThrust;
                        }
                        else if (ThrLocM.Backward == MainLocM.Backward)
                        {
                            BackwardThrusters.Add(Thrust);
                            BackwardThrMax += Thrust.MaxEffectiveThrust;
                        }
                    }
                }

                private void SetGroupThrust(List<IMyThrust> ThrList, float Thr)
                {
                    for (int i = 0; i < ThrList.Count; i++)
                    {
                        //ThrList[i].SetValue("Override", Thr); //OldSchool
                        ThrList[i].ThrustOverridePercentage = Thr;
                    }
                }

                public void SetThrF(Vector3D ThrVec)
                {
                    SetGroupThrust(this, 0f);
                    //X
                    if (ThrVec.X > 0)
                    {
                        SetGroupThrust(RightThrusters, (float)(ThrVec.X / RightThrMax));
                    }
                    else
                    {
                        SetGroupThrust(LeftThrusters, -(float)(ThrVec.X / LeftThrMax));
                    }
                    //Y
                    if (ThrVec.Y > 0)
                    {
                        SetGroupThrust(UpThrusters, (float)(ThrVec.Y / UpThrMax));
                    }
                    else
                    {
                        SetGroupThrust(DownThrusters, -(float)(ThrVec.Y / DownThrMax));
                    }
                    //Z
                    if (ThrVec.Z > 0)
                    {
                        SetGroupThrust(BackwardThrusters, (float)(ThrVec.Z / BackwardThrMax));
                    }
                    else
                    {
                        SetGroupThrust(ForwardThrusters, -(float)(ThrVec.Z / ForwardThrMax));
                    }
                }
                public void SetThrA(Vector3D ThrVec)
                {
                    double PhysMass = myBot.RemCon.CalculateShipMass().PhysicalMass;
                    SetThrF(ThrVec * PhysMass);
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