﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace ToDoMod
{
    class ToDoList : IClickableMenu
    {
        /*********
        ** Properties
        *********/
        /// <summary>
        /// The mod settings.
        /// </summary>
        private readonly ModConfig Config;

        /// <summary>
        /// Saving the mod settings.
        /// </summary>
        private readonly Action SaveConfig;

        /// <summary>
        /// The save file's task list.
        /// </summary>
        private ModData Data;

        /// <summary>
        /// Saving the updated task list.
        /// </summary>
        private readonly Action SaveData;

        /// <summary>
        /// Flag for whether the to do list can be closed.
        /// </summary>
        private bool CanClose;

        /// <summary>
        /// Flag for if the to do list is open.
        /// </summary>
        private bool isOpen;

        /// <summary>
        /// The text box underneath the list.
        /// </summary>
        private TaskType taskType;

        /// <summary>
        /// The font to use for the text box and list - depends on whether user chose to use larger font or not.
        /// </summary>
        private SpriteFont listFont;

        

        /// <summary>
        /// All of the pages and their included tasks.
        /// </summary>
        private List<List<String>> taskPages;

        /// <summary>
        /// The full list of task descriptions filled in from the saved config file.
        /// </summary>
        private List<String> loadedTaskNames;

        /// <summary>
        /// The clickable task buttons on the list.
        /// </summary>
        private List<ClickableComponent> taskPageButtons;
        private int currentPage;

        /// <summary>
        /// Navigation stuff
        /// </summary>
        public const int region_forwardButton = 101;
        public const int region_backButton = 102;
        public ClickableTextureComponent forwardButton;
        public ClickableTextureComponent backButton;
        private int TaskPage = -1;
        public const int tasksPerPage = 5;
        

        /*********
        ** Public methods
        *********/

        public ToDoList(int currentIndex, ModConfig config, Action saveConfig, ModData data, Action saveData) : base(0, 0, 0, 0, true)
        {
            /* Hang onto those mod config and saved task list files */
            this.Config = config;
            this.SaveConfig = saveConfig;
            this.Data = data;
            this.SaveData = saveData;

            /* Position the to do list on screen */
            this.width = Game1.tileSize * 13;
            this.height = Game1.tileSize * 9;
            Vector2 centeringOnScreen = Utility.getTopLeftPositionForCenteringOnScreen(this.width, this.height, 0, 0);
            this.xPositionOnScreen = (int)centeringOnScreen.X;
            this.yPositionOnScreen = Game1.viewport.Height / 2 - (700 + IClickableMenu.borderWidth * 2) / 2;

            /* Close button */
            this.upperRightCloseButton = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width - 2 * Game1.pixelZoom, this.yPositionOnScreen + 15 * Game1.pixelZoom, 12 * Game1.pixelZoom, 12 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), (float)Game1.pixelZoom, false);

            /* Determine the font to use from the config file. */
            if (this.Config.UseLargerFont)
            {
                this.listFont = Game1.dialogueFont;
            }
            else
            {
                this.listFont = Game1.smallFont;
            }

            /* Create the text box and its confirmation button */
            taskType = new TaskType(this.listFont);
            
            /* Load in the saved tasks */
            this.loadedTaskNames = new List<String>();
            loadTaskList();

            /* Create the clickable task buttons on the list */
            this.taskPageButtons = new List<ClickableComponent>();
            /* Counter to go through the full list of loaded names */
            int taskNameCount = 0;
            for (int index = 0; index < tasksPerPage; ++index)
            {
                List<ClickableComponent> taskPageButtons = this.taskPageButtons;
                ClickableComponent clickableComponent = new ClickableComponent(new Rectangle(this.xPositionOnScreen + Game1.tileSize / 2, this.yPositionOnScreen + Game1.tileSize / 4 + index * ((this.height - Game1.tileSize / 2) / 6) + (this.height - Game1.tileSize) / 6 + Game1.pixelZoom - 12, this.width - Game1.tileSize / 2, (this.height - Game1.tileSize / 2) / 6 + Game1.pixelZoom), string.Concat((object)index));
                clickableComponent.myID = index;
                clickableComponent.downNeighborID = -7777;
                int num1 = index > 0 ? index - 1 : -1;
                clickableComponent.upNeighborID = num1;
                int num2 = -7777;
                clickableComponent.rightNeighborID = num2;
                int num3 = -7777;
                clickableComponent.leftNeighborID = num3;
                int num4 = 1;
                clickableComponent.fullyImmutable = num4 != 0;

                taskPageButtons.Add(clickableComponent);
                ++taskNameCount;
            }

            /* Create the clickable navigation buttons */
            ClickableTextureComponent textureComponent1 = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen - Game1.tileSize, this.yPositionOnScreen + this.height - 12 * Game1.pixelZoom, 12 * Game1.pixelZoom, 11 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(352, 495, 12, 11), (float)Game1.pixelZoom, false);
            int num5 = 102;
            textureComponent1.myID = num5;
            int num6 = -7777;
            textureComponent1.rightNeighborID = num6;
            this.backButton = textureComponent1;
            ClickableTextureComponent textureComponent2 = new ClickableTextureComponent(new Rectangle(this.xPositionOnScreen + this.width + Game1.tileSize - 12 * Game1.pixelZoom, this.yPositionOnScreen + this.height - 12 * Game1.pixelZoom, 12 * Game1.pixelZoom, 11 * Game1.pixelZoom), Game1.mouseCursors, new Rectangle(365, 495, 12, 11), (float)Game1.pixelZoom, false);
            int num7 = 101;
            textureComponent2.myID = num7;
            this.forwardButton = textureComponent2;
            
            /* Split the tasks into pages */
            pageTasks();

            /* The to do list is open for business */
            isOpen = true;
            

        }

        /// <summary>
        /// Reload the current task list into the correct buttons and pages after a change.
        /// </summary>
        private void reload()
        {
            loadTaskList();
            pageTasks();
        }

        /// <summary>
        /// Load the full list of saved tasks from the config file.
        /// </summary>
        private void loadTaskList()
        {
            loadedTaskNames = this.Data.SavedTasks.Cast<String>().ToList();
        }

        /// <summary>
        /// Split the loaded tasks across multiple pages.
        /// </summary>
        private void pageTasks()
        {
            this.taskPages = new List<List<String>>();
            
            /* Load the tasks onto the page in reverse order, oldest at the bottom */
            /* Oldest = index 0 in the saved list */
            for (int index = loadedTaskNames.Count - 1; index >= 0; --index)
            {
                int num = loadedTaskNames.Count - 1 - index;
                if (this.taskPages.Count <= num / tasksPerPage)
                    this.taskPages.Add(new List<String>());
                this.taskPages[num / tasksPerPage].Add(loadedTaskNames[index]);
            }
            
            
            /* Debug - tasks loaded in newest at the bottom */
            /*
            for (int index = 0; index < loadedTaskNames.Count; ++index)
            {
                int num = index;
                if (this.taskPages.Count <= num / tasksPerPage)
                    this.taskPages.Add(new List<String>());

                this.taskPages[num / tasksPerPage].Add(loadedTaskNames[index]);
            }
            */

            this.currentPage = Math.Min(Math.Max(this.currentPage, 0), this.taskPages.Count - 1);
            this.TaskPage = -1;
        }

        /* Not used but needs implementing */
        public override void receiveRightClick(int x, int y, bool playSound = true)
        {

        }

        /// <summary>
        /// Move the list of tasks forward one page.
        /// </summary>
        private void taskPageForwardButton()
        {
            this.currentPage = this.currentPage + 1;
            Game1.playSound("shwip");
        }

        /// <summary>
        /// Move the list of tasks backward one page.
        /// </summary>
        private void taskPageBackButton()
        {
            this.currentPage = this.currentPage - 1;
            Game1.playSound("shwip");
        }

        /// <summary>
        /// Handle clicking on the menu - covers clicking a task, the ok button, and off the menu.
        /// </summary>
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            if (Game1.activeClickableMenu == null)
                return;
            if (this.TaskPage == -1)
            {
                for (int index = 0; index < this.taskPageButtons.Count; ++index)
                {
                    if (this.taskPages.Count > 0 && this.taskPages[this.currentPage].Count > index && this.taskPageButtons[index].containsPoint(x, y))
                    {
                        int valueToRemove = 0;
                        /* If we're on the very first page - task indexes match loaded list easier... */
                        if (this.currentPage == 0)
                        {
                            /* Debug - if new tasks at end */
                            //valueToRemove = index;

                            valueToRemove = loadedTaskNames.Count - 1 - index;
                        }
                        /* If we're on any other page */
                        else
                        {
                            /* Debug - New tasks at end */
                            //valueToRemove = index + tasksPerPage * currentPage;
                            
                            valueToRemove = loadedTaskNames.Count - 1 - index - (tasksPerPage * currentPage);
                        }

                        /* Remove the task at the calculated index and save the updated list to the config file. */
                        this.Data.SavedTasks.RemoveAt(valueToRemove);
                        this.SaveData();
                        /* Reload the list to display the updated one. */
                        this.reload();
                        
                        return;
                    }
                }

                /* If we clicked on the forward button, move the page forwards */
                if (this.currentPage < this.taskPages.Count - 1 && this.forwardButton.containsPoint(x, y))
                    this.taskPageForwardButton();
                /* If we clicked on the back button, move the page back. */
                else if (this.currentPage > 0 && this.backButton.containsPoint(x, y))
                {
                    this.taskPageBackButton();
                }
                /* In case someone selects the text box */
                else if (this.taskType.textBoxCC.containsPoint(x,y))
                {
                    return;
                }
                /* If we clicked the ok button */
                else if (this.taskType.doneNamingButton.containsPoint(x,y))
                {
                    addTask();

                }
                /* Must have clicked off the menu - close it */
                else
                {
                    Game1.playSound("bigDeSelect");
                    this.exitThisMenu(true);
                }

            }
        }

        /// <summary>
        /// Take a key press to either open the menu or type in the text box.
        /// </summary>
        public override void receiveKeyPress(Keys key)
        {
            /* If the to do list is open and we've hit escape or the configured open list key */
            if (((key == Keys.Escape) || key.ToString().Equals(this.Config.OpenListKey)) && this.readyToClose() && this.CanClose)
            {
                isOpen = false;
                CanClose = false;
                Game1.exitActiveMenu();
                return;
            }
            /* If the to do list is open and we've hit enter */
            else if ((isOpen) && (key == Keys.Enter))
            {
                addTask();
            }
            /* We've hit anything else, so ignore it for typing and opening the menu. */
            else
            {
                this.CanClose = true;
                return;
            }
            
        }

        /// <summary>
        /// Add the description typed into the text box as a clickable task on the to do list.
        /// </summary>
        public void addTask()
        {
            /* Only add the task if the text box isn't empty */
            if (!(this.taskType.textBox.Text).Equals(""))
            {
                /* Add the typed text to the list for the config file and save it. */
                this.Data.SavedTasks.Add(taskType.textBox.Text);
                this.SaveData();
                /* Clear the text box for the next thing to be typed in. */
                this.taskType.textBox.Text = "";
                this.reload();
            }
            
        }


        /// <summary>
        /// Draw the to do list components. 
        /// </summary>
        public override void draw(SpriteBatch batch)
        {
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height, false, true);
            batch.End();

            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            
            this.taskType.draw(batch);

            /* Task boxes on this page. */

            Vector2 centeringInTaskBox = Utility.getTopLeftPositionForCenteringOnScreen(this.taskPageButtons[0].bounds.X, this.taskPageButtons[0].bounds.Y, 0, 0); 
            
            if (this.TaskPage == -1)
            {
                for (int index = 0; index < this.taskPageButtons.Count; ++index)
                {
                    if (this.taskPages.Count<List<String>>() > 0 && this.taskPages[this.currentPage].Count<String>() > index)
                    {
                        IClickableMenu.drawTextureBox(batch, Game1.mouseCursors, new Rectangle(384, 396, 15, 15), this.taskPageButtons[index].bounds.X, this.taskPageButtons[index].bounds.Y, this.taskPageButtons[index].bounds.Width - IClickableMenu.borderWidth / 4 - 20, this.taskPageButtons[index].bounds.Height, this.taskPageButtons[index].containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()) ? Color.Wheat : Color.White, (float)Game1.pixelZoom, false);
                        if (this.Config.UseLargerFont)
                        {
                            Utility.drawTextWithShadow(batch, this.taskPages[this.currentPage][index], this.listFont, new Vector2(this.taskPageButtons[index].bounds.X + Game1.tileSize + Game1.pixelZoom - 50, this.taskPageButtons[index].bounds.Y + Game1.pixelZoom * 5), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                        }
                        else
                        {
                            Utility.drawTextWithShadow(batch, this.taskPages[this.currentPage][index], this.listFont, new Vector2(this.taskPageButtons[index].bounds.X + Game1.tileSize + Game1.pixelZoom - 50, this.taskPageButtons[index].bounds.Y + (this.listFont.MeasureString(this.taskPages[this.currentPage][index]).Y / 2 + Game1.tileSize / 4)), Game1.textColor, 1f, -1f, -1, -1, 1f, 3);
                        }

                    }
                }
            }

            /* Navigation buttons if we're midway through pages */
            if (this.currentPage < this.taskPages.Count - 1 && this.TaskPage == -1)
                this.forwardButton.draw(batch);
            if (this.currentPage > 0 || this.TaskPage != -1)
                this.backButton.draw(batch);

            base.draw(batch);

           Game1.mouseCursorTransparency = 1f;
            this.drawMouse(batch);


        }



        
    }
}