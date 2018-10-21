/* v0.37 (1.105 game compatibility + Ability to turn HUD on and off + Fix thruster renaming)  
 In-game script by MMaster  

 Shows all damaged blocks on station/ship on HUD. Also shows blocks which are in construction (when someone grinds them).  
 Automatically renames the blocks when they are damaged and renames them back when they are repaired.  

 Make sure you have active antenna on station/ship otherwise it will not show up on HUD.  
 Also make sure that Programmable Block has same ownership as other blocks otherwise it won't be able to show them on HUD.  

 QUICK GUIDE:  
  1. Load this script to programmable block  
  2. Setup timer block actions: 1. run programmable block 2. start timer block  
  3. Set timer block delay to 3 or more seconds (it will check the blocks at this rate).  
  4. Done  

 If you want to turn it off temporarily / using button:   
  * Run the programmable block with argument "off" (without quotes).   
  * This will automatically rename any damaged blocks back and disable show on hud on them.  

 To turn it back on:   
  * Run the programmable block with argument "on" (without quotes).   

 */
        // set to false if you don't want to show damaged blocks  
        public const bool SHOW_DAMAGED = true;
        // set to false if you don't want to show blocks in construction  
        public const bool SHOW_INCONSTRUCTION = true;

        /*   
        STEAM GROUP  
        I created Steam group where I notify about new things & updates so join if interested.  
        http://steamcommunity.com/groups/mmnews  

        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  
        CHECK MY OTHER MODS (almost all 5 star)  
        http://steamcommunity.com/id/mmaster/myworkshopfiles/?browsefilter=myfiles  
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~  

        Watch my Steam group: http://steamcommunity.com/groups/mmnews  
        Twitter: https://twitter.com/MattsPlayCorner  
        and Facebook: https://www.facebook.com/MattsPlayCorner1080p  
        for more crazy stuff from me in the future :)  

         *   
         * You don't need to modify anything below this!  
         */

        // One word with NO spaces and space on the end!  
        public const string NAME_PREFIX_DAMAGE = "Поврежден: ";
        public const string NAME_PREFIX_CONSTRUCT = "Недостроен: ";



        static bool enabled = true;

        void Main(string argument)
        {
            switch (argument)
            {
                case "on":
                    enabled = true;
                    break;
                case "off":
                    enabled = false;
                    break;
                case "avto":
                    {
                        Runtime.UpdateFrequency = UpdateFrequency.None == Runtime.UpdateFrequency ? UpdateFrequency.Update100 : UpdateFrequency.None;
                        Echo("Autorun is " + (Runtime.UpdateFrequency == UpdateFrequency.None ? "disable" : "enable"));
                    }
                    break;
            }

            List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(Blocks);
            for (int i = 0; i < Blocks.Count; i++)
            {
                IMyTerminalBlock block = Blocks[i];
                IMySlimBlock slim = block.CubeGrid.GetCubeBlock(block.Position);
                int change = 0; // 0 no change, 1 show, 2 hide
                string b_name = block.CustomName;
                bool prefixed = (b_name.StartsWith(NAME_PREFIX_DAMAGE)
                    || b_name.StartsWith(NAME_PREFIX_CONSTRUCT));
                bool should_show = false;

                if (block is IMyThrust)
                {
                    int idx = b_name.LastIndexOf('(') - 1;
                    if (idx >= 0)
                        b_name = b_name.Substring(0, idx);
                }

                if (SHOW_DAMAGED && enabled)
                {
                    if (slim.CurrentDamage > 0)
                    {
                        should_show = true;
                        if (!prefixed)
                        {
                            block.CustomName = NAME_PREFIX_DAMAGE + b_name;
                            change = 1;
                            prefixed = true;
                        }
                    }
                }

                if (SHOW_INCONSTRUCTION && enabled && !should_show)
                {
                    if (slim.BuildIntegrity < slim.MaxIntegrity)
                    {
                        should_show = true;
                        if (!prefixed)
                        {
                            block.CustomName = (NAME_PREFIX_CONSTRUCT + b_name);
                            change = 1;
                            prefixed = true;
                        }
                    }
                }

                if (!should_show && prefixed)
                {
                    block.CustomName = b_name.Substring(b_name.IndexOf(' ') + 1);
                    change = 2;
                    prefixed = false;
                }

                switch (change)
                {
                    case 1:
                        if (!block.ShowOnHUD)
                            block.SetValueBool("ShowOnHUD", true);
                        break;
                    case 2:
                        if (block.ShowOnHUD)
                            block.SetValueBool("ShowOnHUD", false);
                        break;
                }
            }
        }