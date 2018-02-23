/*--------------------------------------------------------------------------    
        AUTHOR: MrAndrey_ka (Ukraine, Cherkassy) e-mail: MyAndrey_ka@mail.ru     
        When using and disseminating information about the authorship is obligatory    
        При использовании и распространении информация об авторстве обязательна    
        ----------------------------------------------------------------------*/
    Program()
    {
        if (!Storage.StartsWith("AntensClient_2"))//проверяем мы ли туда данные записали   
            return;

        var ids = Storage.Split('\n');
        var len = ids.Length;
        for (int i = 1; i < len - 1; i++)
            Antens.Add(new Antena(GridTerminalSystem.GetBlockWithId(long.Parse(ids[i])) as IMyFunctionalBlock));
        Show();
    }

    void Save()
    {
        StringBuilder res = new StringBuilder("AntensClient_2\n");
        Antens.ForEach(x => res.AppendLine(x.Block.GetId().ToString()));
        Storage = res.ToString();
    }

    List<Antena> Antens = new List<Antena>();
    string LS;
    int defch = 1, LA = 0;

    public void Main(string ARGUMENT, UpdateType UT)
    {
        if (UT == UpdateType.Once) SendMes();
        else if (UT < UpdateType.Update1)
        {
            if (ARGUMENT.StartsWith("SET")) { Add(ARGUMENT.Split('_')); return; }// Установка блока: SET_имя блока  
            else if (ARGUMENT.StartsWith("GET")) { Show(); return; }// Отображение антенн: GET  
            else if (ARGUMENT.StartsWith("CHANEL")) // Установка канала по умолчанию: CHANEL_номер канала   
            {
                int g;
                if (!int.TryParse(ARGUMENT.Substring(7), out g)) { Echo("Введено не верное значение"); return; }
                if (g == 0) { Echo("Канал не может иметь значение \"0\""); return; }
                defch = g;
                return;
            }
        }

        if (Antens.Count == 0) { Echo("не установлены антены"); return; }

        int chanal;
        if (ARGUMENT.Length == 3 && ARGUMENT.ToLower() == "sos")
        { chanal = 0; ARGUMENT = MyGPS.GPS(Antens[0].Block.CubeGrid.CustomName, Antens[0].Block.GetPosition()); }
        else
        {
            var pos = ARGUMENT.IndexOf(' ');

            if (pos >= 0 && int.TryParse(ARGUMENT.Substring(0, pos), out chanal))
            { if (chanal == 0) chanal = defch; ARGUMENT = ARGUMENT.Remove(0, pos + 1); }
            else chanal = defch;
        }
        if (ARGUMENT.StartsWith("~"))// Отправка сообщения с панели
        {
            var p = GridTerminalSystem.GetBlockGroupWithName(ARGUMENT.Substring(1)) as IMyTextPanel;
            if (p == null) { Echo("Не найдена панель"); return; }
            ARGUMENT = p.GetPublicText();
        }
        if (string.IsNullOrEmpty(ARGUMENT)) return;//Пустое сообщение
        LS = chanal.ToString() + "¤" + ARGUMENT;
        LA = 0;
        SendMes();
    }

    void SendMes()
    {
        var x = Antens[LA];
        if (!x.Transmit(LS)) Echo("Не удалось отправить сообщение c " + x.Block.CustomName);
        LA++;
        if (Antens.Count == LA) { LA = 0; return; }
        Runtime.UpdateFrequency = UpdateFrequency.Once;
    }

    void Add(string[] Args)
    {
        if (Args.Length != 2) { Echo("Не верные параметры!"); return; }

        var TP = GridTerminalSystem.GetBlockWithName(Args[1]) as IMyFunctionalBlock;//ищем паенль   
        var i = Antens.FindIndex(x => x.Block == TP);

        Antena tmp = i < 0 ? new Antena(TP) : Antens[i];
        if (tmp.Block == null)

            if (Args[0] == "SET-") // Удаляем блок  
            {
                if (i > 0)
                    Antens.Remove(tmp);
                Show();
                return;
            }

        if (i < 0) Antens.Add(tmp);
        else Antens[i] = tmp;
        Show();
    }

    void Show(){Echo("Панели:"); Antens.ForEach(x => Echo(x.Block.CustomName));}

    public struct Antena
    {
        IMyFunctionalBlock Ant;
        bool LA;
        public IMyFunctionalBlock Block { get { return Ant; } }
        public Antena(IMyFunctionalBlock Bloc) { LA = Bloc is IMyLaserAntenna; Ant = LA || (Bloc is IMyRadioAntenna) ? Bloc : null; }
        public bool Transmit(string Text)
        {
            if (Ant is IMyLaserAntenna) return (Ant as IMyLaserAntenna).TransmitMessage(Text);
            else return (Ant as IMyRadioAntenna).TransmitMessage(Text, MyTransmitTarget.Default);
        }
    }
    static class MyGPS
    {
        public static string GPS(string Name, Vector3D Val)
        { return string.Format("GPS:{0}:{1:0.#}:{2:0.#}:{3:0.#}:", Name, Val.GetDim(0), Val.GetDim(1), Val.GetDim(2)); }
        public static string GPS(string Name, Vector3D Val, Vector3D Pos, int dist) { return GPS(Name, Pos + Val * dist); }
        public static string Vec_GPS(string Name, Vector3D Val, Vector3D Pos, int dist)
        { Pos += Val * dist; return Name + Val + "\n" + GPS(Name, Pos + Val * dist); }
    }