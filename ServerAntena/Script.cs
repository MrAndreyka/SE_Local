/*----------------------------------------------------------------------  
AUTHOR: MrAndrey_ka (Ukraine Cherkassy) e-mail: Andrey.ck.ua@gmail.com  
When using and disseminating information about the authorship is obligatory  
При использовании и распространении информация об авторстве обязательна  
----------------------------------------------------------------------*/

    Program()
    {
        if (!Storage.StartsWith("TextPanelsServ2"))//проверяем мы ли туда данные записали   
        { Echo("Внимание! Установите пароль перед использованием, иначе его могут установить за вас!"); return; }

        var ids = Storage.Remove(0, 15).Split('\n');
        var len = ids.GetLength(0);

        if (len < 3) return;

        for (int i = 1; i < len - 1; i += 2)
            Panels.Add(new PC(GridTerminalSystem.GetBlockWithId(long.Parse(ids[i + 1])) as IMyTextPanel,
                 int.Parse(ids[i])));

        Echo("Данные о LCD восстановлены");
    }

    void Save()
    {
        StringBuilder res = new StringBuilder("\n");
        Panels.ForEach(x => res.AppendLine(x.key.ToString() + "\n" + x.TP.GetId()));
        Storage = "TextPanelsServ2" + res.Replace("\r\n", "\n").ToString();
    }

    public struct PC
    {
        public int key;
        public IMyTextPanel TP;
        public PC(IMyTextPanel panel, int key = 0) { TP = panel; this.key = key; }
    }

    List<PC> Panels = new List<PC>();

    public void Main(string ARGUMENT, UpdateType UT)
    {
        if (UT != UpdateType.Antenna)
        {
            if (ARGUMENT.StartsWith("SET")) // Установка панели с привязкой к каналу: SET_имя панели_номер канала   
                AddPanel(ARGUMENT.Split('_')); // Удаление панели: SET-_имя панели_0    Установка пароля: SET-PASS_пароль
            else if (ARGUMENT.StartsWith("GET")) // Установка панели с привязкой к каналу: SET_имя панели_номер канала   
                ShowPanels();
            else Echo("Левая команда");
            return;
        }

        //Прием сообщения   
        var args = ARGUMENT.Split('¤');

        string TIME = DateTime.Now.ToString(@"HH:mm") + ":  ";

        if (args.GetLength(0) != 2)
        { Echo(TIME + ARGUMENT); return; } // Сообщение отправлено левым клиентом и структура сообщения скорее всего не верна   

        int chanel;
        if (!int.TryParse(args[0], out chanel)) { Echo("Не верный канал"); return; }// Сообщение отправлено скорее всего левым клиентом либо где-то ошибка   

        if (chanel == 0)//Это сос 
            Panels.ForEach(x => ShowMes(TIME + " \"SOS\" сообщение по координатам:\n", args[1], x.TP));

        var tmp = Panels.Find(x => x.key == chanel);
        if (tmp.TP == null) Echo(TIME + args[1]);// Панель для этого канала не определена   
        else ShowMes(TIME, args[1], tmp.TP);
    }

    void ShowMes(string Time, string Text, IMyTextPanel Pan)
    {
        Pan.WritePublicText(Time + Text, false);
        Pan.CustomData += "\n" + Time + Text;
        Pan.ShowTextureOnScreen();
        Pan.ShowPublicTextOnScreen();
    }

    void AddPanel(string[] Args)
    {
        if (Args.Length != 3) { Echo("Не верные параметры!"); return; }

        var TP = GridTerminalSystem.GetBlockWithName(Args[1]) as IMyTextPanel;//ищем паенль   
        if (TP == null) { Echo("Не найдена панель: " + Args[1]); return; }

        var i = Panels.FindIndex(x => x.TP == TP);

        PC tmp = i < 0 ? new PC(TP) : Panels[i];

        if (Args[0] == "SET-") // Удаляем панель  
        {
            if (i >= 0) Panels.Remove(tmp);
            ShowPanels();
            return;
        }

        if (!int.TryParse(Args[2], out tmp.key) || tmp.key == 0) { Echo("Не верное значение канала"); return; }// если канал введен не верно    
                                                                                                               // можно проверять чтобы канал был всегда больше 0   
        if (i < 0) Panels.Add(tmp);
        else Panels[i] = tmp;
        ShowPanels();
    }

    void ShowPanels() { Echo("Панели:"); Panels.ForEach(x => Echo(x.key.ToString() + " " + x.TP.CustomName)); }
