        /* Клан "Андромеда" (https://vk.com/andromeda_se)   
 * LCD-инвентарь: версия #01-05-2017   
 *   
 * Автор: MoryakSPb (https://vk.com/moryakspb)  
 *    
 * Справка:   
 * Для начала работы вам необходимо:   
 * 1. Программный блок с данным скриптом   
 * 2. Таймер, который будет запускать пр. блок и самого себя   
 * 3. LCD-Панель для вывода   
 * 4. Группа с блоками, которые содержат инвентарь   
 *    
 * Дополнительную информацию см. на странице скрипта в Workshop:  
 * http://steamcommunity.com/sharedfiles/filedetails/?id=869904405 
 */ 
 
 
        const string LCD_PANEL_NAME = "Wide LCD panel 3"; 
 
        const bool USING_GROUP = false; 
        const string INVENTORY_NAME = "Small Cargo Container 1"; 
 
        readonly IMyTextPanel TextPanel; 
        readonly List<IMyInventory> Inventories = new List<IMyInventory>(); 
        readonly List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>(); 
 
        //bool LOW_ENERGY_LEVEL = false; 
 
        Program() 
        { 
            TextPanel = GridTerminalSystem.GetBlockWithName(LCD_PANEL_NAME) as IMyTextPanel; 
 
            var BLOCKS = new List<IMyTerminalBlock>(); 
            if (USING_GROUP) GridTerminalSystem.GetBlockGroupWithName(INVENTORY_NAME).GetBlocks(BLOCKS); else BLOCKS.Add(GridTerminalSystem.GetBlockWithName(INVENTORY_NAME)); 
            foreach (var item in BLOCKS) 
            { 
                for (int i = 0; i < item.InventoryCount; i++) 
                { 
                    Inventories.Add(item.GetInventory(i)); 
                } 
            } 
        } 
            public void Main(string ARGUMENT) 
        { 
            StringBuilder LCD_TEXT = new StringBuilder(); 
 
            var ALL_ITEMS = new Dictionary<String, VRage.MyFixedPoint>(); 
            VRage.MyFixedPoint MAX_VOLUME = 0; 
            VRage.MyFixedPoint CURRENT_VOLUME = 0; 
            foreach (var Inventory in Inventories) 
            { 
                MAX_VOLUME += Inventory.MaxVolume; 
                CURRENT_VOLUME += Inventory.CurrentVolume; 
                foreach (var item in Inventory.GetItems()) 
                { 
                    string ITEM_NAME = item.Content.SubtypeName; 
                    var ITEM_TYPE = item.Content.TypeId.ToString(); 
                    switch (ITEM_TYPE) //добавление к имени метки типа. Нужно для переводчика  
                    { 
                        case "MyObjectBuilder_Ore": 
                            ITEM_NAME += "_Ore"; 
                            break; 
                        case "MyObjectBuilder_Ingot": 
                            ITEM_NAME += "_Ingot"; 
                            break; 
                        case "MyObjectBuilder_Component": 
                            ITEM_NAME += "_Component"; 
                            break; 
                        case "MyObjectBuilder_GasContainerObject": 
                            BOTTLE: 
                            ITEM_NAME += "_Bottle"; 
                            break; 
                        case "MyObjectBuilder_OxygenContainerObject": 
                            goto BOTTLE; 
                        case "MyObjectBuilder_AmmoMagazine": 
                            ITEM_NAME += "_Ammo"; 
                            break; 
                        case "MyObjectBuilder_PhisycalGunObject": 
                            ITEM_NAME += "_Tool"; 
                            break; 
                    } 
 
 
                    if (!ALL_ITEMS.ContainsKey(ITEM_NAME)) 
                        ALL_ITEMS[ITEM_NAME] = item.Amount; 
                    else 
                        ALL_ITEMS[ITEM_NAME] += item.Amount; 
                } 
            } 
            LCD_TEXT.AppendLine("Заполнено на " + Math.Round((decimal)CURRENT_VOLUME / (decimal)MAX_VOLUME * 100, 2, MidpointRounding.AwayFromZero) + '%'); 
            LCD_TEXT.AppendLine(); 
 
            foreach (var ITEM in ALL_ITEMS) 
            { 
                LCD_TEXT.Append(TRANSLATE(ITEM.Key)); 
                for (int i = 0; i < Convert.ToInt32(53 / TextPanel.GetValue<Single>("FontSize") - TRANSLATE(ITEM.Key).Length - Math.Round((decimal)ITEM.Value, 0, MidpointRounding.AwayFromZero).ToString().Length); i++) 
                { 
                    LCD_TEXT.Append('_'); 
                } 
                LCD_TEXT.Append(Math.Round((decimal)ITEM.Value, 0, MidpointRounding.AwayFromZero)); 
                LCD_TEXT.AppendLine(); 
                 
            } 
            TextPanel.WritePublicText(LCD_TEXT); 
        } 
        string TRANSLATE(string CODE) 
        { 
            switch (CODE) 
            { 
                case "Stone_Ore": 
                    return "Камень"; 
                case "Iron_Ore": 
                    return "Fe_руда"; 
                case "Nickel_Ore": 
                    return "Ni_руда"; 
                case "Cobalt_Ore": 
                    return "Co_руда"; 
                case "Magnesium_Ore": 
                    return "Mg_руда"; 
                case "Silicon_Ore": 
                    return "Si_руда"; 
                case "Silver_Ore": 
                    return "Ag_руда"; 
                case "Gold_Ore": 
                    return "Au_руда"; 
                case "Platinum_Ore": 
                    return "Pt_руда"; 
                case "Uranium_Ore": 
                    return "U_руда"; 
                case "Ice_Ore": 
                    return "Лед"; 
                case "Scrap_Ore": 
                    return "Металолом"; 
 
                case "Stone_Ingot": 
                    return "Гравий"; 
                case "Iron_Ingot": 
                    return "Fe"; 
                case "Nickel_Ingot": 
                    return "Ni"; 
                case "Cobalt_Ingot": 
                    return "Co"; 
                case "Magnesium_Ingot": 
                    return "Mg"; 
                case "Silicon_Ingot": 
                    return "Si"; 
                case "Silver_Ingot": 
                    return "Ag"; 
                case "Gold_Ingot": 
                    return "Au"; 
                case "Platinum_Ingot": 
                    return "Pt"; 
                case "Uranium_Ingot": 
                    return "U"; 
 
                case "BulletproofGlass_Component": 
                    return "Стекло"; 
                case "Computer_Component": 
                    return "Компьютер"; 
                case "Construction_Component": 
                    return "К. конструкции"; 
                case "Detector_Component": 
                    return "К. детектора"; 
                case "Display_Component": 
                    return "Дисплей"; 
                case "Explosives_Component": 
                    return "Взрывчатка"; 
                case "Girder_Component": 
                    return "Балка"; 
                case "GravityGenerator_Component": 
                    return "К. грав. ген."; 
                case "InteriorPlate_Component": 
                    return "Инт. Пластина"; 
                case "LargeTube_Component": 
                    return "Б. труба"; 
                case "Medical_Component": 
                    return "Мед. К."; 
                case "MetalGrid_Component": 
                    return "М. решетка"; 
                case "Missile200mm_Ammo": 
                    return "Ракета"; 
                case "Motor_Component": 
                    return "Мотор"; 
                case "NATO_25x184mm_Ammo": 
                    return "25x184mm"; 
                case "NATO_5p56x45mm_Ammo": 
                    return "5,56x45mm"; 
                case "PowerCell_Component": 
                    return "Аккамулятор"; 
                case "RadioCommunication_Component": 
                    return "К. Радио"; 
                case "Reactor_Component": 
                    return "К. реактора"; 
                case "SmallTube_Component": 
                    return "М. труба"; 
                case "SolarCell_Component": 
                    return "Фото-элемент"; 
                case "SteelPlate_Component": 
                    return "Ст. пластина"; 
                case "Superconductor_Component": 
                    return "Сверхпроводник"; 
                case "Thrust_Component": 
                    return "К. ускорителя"; 
                case "Canvas_Component": 
                    return "Ткань"; 
 
                case "WelderItem": 
                    return "Сварщик"; 
                case "Welder2Item": 
                    return "Сварщик_2lvl"; 
                case "Welder3Item": 
                    return "Сварщик_3lvl"; 
                case "Welder4Item": 
                    return "Сварщик_4lvl"; 
                case "AngleGrinderItem": 
                    return "Болгарка"; 
                case "AngleGrinder2Item": 
                    return "Болгарка_2lvl"; 
                case "AngleGrinder3Item": 
                    return "Болгарка_3lvl"; 
                case "AngleGrinder4Item": 
                    return "Болгарка_4lvl"; 
                case "HandDrillItem": 
                    return "Р. бур"; 
                case "HandDrill2Item": 
                    return "Р. бур_2lvl"; 
                case "HandDrill3Item": 
                    return "Р. бур_3lvl"; 
                case "HandDrill4Item": 
                    return "Р. бур_4lvl"; 
                case "AutomaticRifleItem": 
                    return "Автомат"; 
                case "PreciseAutomaticRifleItem": 
                    return "Автомат_прц"; 
                case "RapidFireAutomaticRifleItem": 
                    return "Автомат_cкр"; 
                case "UltimateAutomaticRifleItem": 
                    return "Автомат_элт"; 
                case "HydrogenBottle_Bottle": 
                    return "Балон_H2"; 
                case "OxygenBottle_Bottle": 
                    return "Балон_O2"; 
 
                case "Scrap_Ingot": 
                    return "Ст. металолом"; 
                case "Organic_Ore": 
                    return "Органика"; 
 
                default: 
                    return CODE; 
            } 
        } 
