using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Nodes;

using System.Threading.Tasks;
using System.Threading;
using RestSharp;
using SextantHelper.Data;
using System.Net.WebSockets;
using System.Numerics;

namespace SextantHelper
{
    public class Main : BaseSettingsPlugin<MainSettings>
    {

        internal SextantRepository sextantRepo;
        public override bool Initialise()
        {
            Name = "Sextant Helper";
            sextantRepo = new SextantRepository(Settings);
            return base.Initialise();
        }


        DateTime nextInventoryCache = DateTime.Now;
        DateTime nextSextantRoll = DateTime.Now;
        Random random = new Random();

        InventoryHolder? server_inventory = null;
        int sextantsUsed = 0;
        double chaosEstimate = 0;
        bool isRunning = false;
        private void StopCoroutine(string routineName)
        {
            var routine = Core.ParallelRunner.FindByName(routineName);
            routine?.Done();
        }

        public override Job Tick()
        {
            if (DateTime.Now > nextInventoryCache)
            {
                server_inventory = GameController.Game.IngameState.Data.ServerData.PlayerInventories[0];
                nextInventoryCache = DateTime.Now.AddSeconds(1);
            }


            if (Settings.Roll_Sextant_Hotkey.PressedOnce())
            {
                sextantState = SextantState.Voidstone_Evaluate;
                isRunning = !isRunning;

                if (isRunning)
                {
                    sextantsUsed = 0;
                    chaosEstimate = 0;
                }
            }

            if (isRunning)            
                SextantRollingLogic();

            return null;
        }


        SextantState sextantState = SextantState.Voidstone_Evaluate;
        enum SextantState
        {
            Voidstone_Evaluate,
            Sextant_Select,
            Sextant_Use,
            Compass_Select,
            Compass_Use,
            Compass_Deposit
        }

        void SextantRollingLogic()
        {
            if (nextSextantRoll > DateTime.Now)
                return;

            nextSextantRoll = DateTime.Now.AddMilliseconds(Settings.Base_Mouse_Delay + random.Next(25));

            switch (sextantState)
            {
                case SextantState.Voidstone_Evaluate:
                    {
                        //Before we start, lets confirm there is open inventory space. If not stop the whole script. 
                        var openSlot = GetOpenInventoryLocation();
                        if (openSlot == null)
                        {
                            isRunning = false;
                            return;
                        }

                        var targetPos = new Vector2(980, 925);
                        if (targetPos.DistanceSquared(new Vector2(Input.MousePosition.X, Input.MousePosition.Y)) > 30)
                        {
                            Input.SetCursorPos(targetPos + new Vector2(random.Next(-3, 3), random.Next(-3, 3)));
                            nextSextantRoll = DateTime.Now.AddMilliseconds(50);
                        }
                        else
                        {
                            var sextant = GetMouseoverSextant();
                            if (sextant == null || sextant.Price < Settings.Min_Chaos_Value)
                                sextantState = SextantState.Sextant_Select;
                            else
                                sextantState = SextantState.Compass_Select;
                        }
                        break;
                    }
                case SextantState.Sextant_Select:
                    {
                        var targetPos = new Vector2(430, 400);
                        if (targetPos.DistanceSquared(new Vector2(Input.MousePosition.X, Input.MousePosition.Y)) > 30)
                        {
                            Input.SetCursorPos(targetPos + new Vector2(random.Next(-3, 3), random.Next(-3, 3))); 
                            nextSextantRoll = DateTime.Now.AddMilliseconds(50);
                        }
                        else
                        {
                            var itemLines = GetMouseoverItemLines();
                            //We have no sextants remaining. Go ahead and stop everything.
                            if (itemLines == null || itemLines.Count < 3 || !itemLines.Any(I=>I.Trim() == "Awakened Sextant"))
                            {
                                sextantState = SextantState.Voidstone_Evaluate;
                                isRunning = false;
                                return;
                            }

                            Input.Click(MouseButtons.Right);
                            sextantState = SextantState.Sextant_Use;
                        }
                        break;
                    }
                case SextantState.Sextant_Use:
                    {
                        var targetPos = new Vector2(980, 925);
                        if (targetPos.DistanceSquared(new Vector2(Input.MousePosition.X, Input.MousePosition.Y)) > 30)
                        {
                            Input.SetCursorPos(targetPos + new Vector2(random.Next(-3, 3), random.Next(-3, 3))); 
                            nextSextantRoll = DateTime.Now.AddMilliseconds(50);
                        }
                        else
                        {
                            var itemLines = GetMouseoverItemLines();
                            if (itemLines == null || itemLines.Count < 2)
                            {
                                sextantState = SextantState.Voidstone_Evaluate;
                                isRunning = false;
                                return;
                            }
                            var sextant = GetMouseoverSextant();
                            if (sextant == null || sextant.Price < Settings.Min_Chaos_Value)
                            {
                                sextantsUsed++;
                                Input.Click(MouseButtons.Left);
                                sextantState = SextantState.Voidstone_Evaluate;
                            }
                            //There was a delay. This should actually be stored. 
                            else if (sextant.Price >= Settings.Min_Chaos_Value)                            
                                sextantState = SextantState.Compass_Select;                            
                            else
                            {
                                
                                sextantState = SextantState.Voidstone_Evaluate;
                                isRunning = false;
                                return;
                            }
                        }
                        break;
                    }

                case SextantState.Compass_Select:
                    {
                        var targetPos = new Vector2(150, 600);
                        if (targetPos.DistanceSquared(new Vector2(Input.MousePosition.X, Input.MousePosition.Y)) > 30)
                        {
                            Input.SetCursorPos(targetPos + new Vector2(random.Next(-3, 3), random.Next(-3, 3)));
                            nextSextantRoll = DateTime.Now.AddMilliseconds(50);
                        }
                        else
                        {
                            var itemLines = GetMouseoverItemLines();
                            //We have no compasses remaining. Go ahead and stop everything.
                            if (itemLines == null || itemLines.Count < 3 || !itemLines.Any(I => I.Trim() == "Surveyor's Compass"))
                            {
                                sextantState = SextantState.Voidstone_Evaluate;
                                isRunning = false;
                                return;
                            }

                            Input.Click(MouseButtons.Right);
                            sextantState = SextantState.Compass_Use;
                        }
                        break;
                    }
                case SextantState.Compass_Use:
                    {
                        var targetPos = new Vector2(980, 925);
                        if (targetPos.DistanceSquared(new Vector2(Input.MousePosition.X, Input.MousePosition.Y)) > 30)
                        {
                            Input.SetCursorPos(targetPos + new Vector2(random.Next(-3, 3), random.Next(-3, 3)));
                            nextSextantRoll = DateTime.Now.AddMilliseconds(50);
                        }
                        else
                        {
                            var sextant = GetMouseoverSextant();
                            if (sextant == null || sextant.Price < Settings.Min_Chaos_Value)
                            {
                                Input.Click(MouseButtons.Right);
                                sextantState = SextantState.Voidstone_Evaluate;
                                nextSextantRoll = DateTime.Now;
                            }
                            else
                            {
                                chaosEstimate += sextant.Price - 1;
                                Input.Click(MouseButtons.Left);
                                sextantState = SextantState.Compass_Deposit;
                            }
                        }
                        break;
                    }
                case SextantState.Compass_Deposit:
                    {
                        var openSlot = GetOpenInventoryLocation();
                        //No inventory slots exist. Stop rolling
                        if (openSlot == null)
                        {
                            sextantState = SextantState.Voidstone_Evaluate;
                            isRunning = false;
                            return;
                        }
                        if (openSlot.Value.DistanceSquared(new Vector2(Input.MousePosition.X, Input.MousePosition.Y)) > 30)
                        {
                            Input.SetCursorPos(openSlot.Value + new Vector2(random.Next(-3, 3), random.Next(-3, 3)));
                            nextSextantRoll = DateTime.Now.AddMilliseconds(50);
                        }
                        else
                        {
                            Input.Click(MouseButtons.Left);
                            sextantState = SextantState.Sextant_Select;
                        }
                        break;
                    }

            }
        }

        Vector2? GetOpenInventoryLocation()
        {
            for (var y = 0; y < 5; y++)
                for (var x = 0; x < 12; x++)
                {
                    var existingItem = server_inventory.Inventory.InventorySlotItems.Any(I => I.PosX == x && I.PosY == y);
                    if (!existingItem)
                        return new Vector2(1275 + x * 53 + 22, 590 + y * 53 + 22);
                }

            return null;
        }

        public override void Render()
        {
            Graphics.DrawText($"{sextantRepo.Sextants.Count()} Mapped Sextant Lookups", new Vector2(100, 100));

            var totalWeights = sextantRepo.Sextants.Sum(I => I.Weights_Total);
            var expectedReturns = sextantRepo.Sextants.Where(I => I.Price >= Settings.Min_Chaos_Value).Sum(I => I.Weights_Total * I.Price / totalWeights)-1;
            var expectedBlockedReturns = sextantRepo.Sextants.Where(I => I.Price >= Settings.Min_Chaos_Value).Sum(I => I.Weights_Total * I.Price / (totalWeights-4000))-1;

            Graphics.DrawText($"Expected Un-Blocked Returns: {expectedReturns.ToString("N2")}c", new Vector2(100, 115));
            Graphics.DrawText($"Expected Blocked Returns: {expectedBlockedReturns.ToString("N2")}c", new Vector2(100, 130));
            Graphics.DrawText($"Sextants Used: {sextantsUsed} Est Chaos: {chaosEstimate.ToString("N2")}c Profit: { (chaosEstimate - sextantsUsed * Settings.Sextant_Cost).ToString(("N2"))}", new Vector2(100, 145));


            var match = GetMouseoverSextant();
            if (match != null)
            {
                var gameRect = GameController.Window.GetWindowRectangle();
                var renderRect = new SharpDX.RectangleF(gameRect.Center.X - 150, gameRect.Height - 75, 300, 40);
                Graphics.DrawBox(renderRect, match.Price >= Settings.Min_Chaos_Value ? SharpDX.Color.DarkGreen : SharpDX.Color.DarkRed, 5);
                Graphics.DrawText($"Value: {match.Price}", new Vector2(renderRect.Center.X, renderRect.Y), SharpDX.Color.White, ExileCore.Shared.Enums.FontAlign.Center);
            }
        }

        void RenderInventoryTest()
        {
            if (DateTime.Now > nextInventoryCache)
            {
                server_inventory = GameController.Game.IngameState.Data.ServerData.PlayerInventories[0];
                nextInventoryCache = DateTime.Now.AddSeconds(1);
            }

            if (server_inventory == null)
                return;
            for(var y = 0; y < 5;y++)
                for(var x = 0; x < 12; x++)
                {
                    var existingItem = server_inventory.Inventory.InventorySlotItems.Any(I=>I.PosX == x && I.PosY == y);
                    Graphics.DrawFrame(new SharpDX.RectangleF(1275 + x*53, 590+y*53,50, 50), existingItem ? SharpDX.Color.Red : SharpDX.Color.Green,1);                    
                }
        }


        public List<string>? GetMouseoverItemLines()
        {
            var uiHover = GameController.Game.IngameState.UIHover;
            if (uiHover?.Address == 0) return null;
            var inventoryItemIcon = uiHover?.AsObject<HoverItemIcon>();
            if (inventoryItemIcon?.Tooltip == null)
                return null;

            var tooltip = inventoryItemIcon.Tooltip;

            if (tooltip == null) return null;

            return FlattenElement(tooltip);
        }

        public List<string> FlattenElement(Element label)
        {
            var results = new List<string>();
            var str = label.GetTextWithNoTags(512);

            //We MUST check null before running trim or it will throw exceptions.
            if (!string.IsNullOrEmpty(str))
            {
                str = str.Replace("\r", "").Replace("\n", "").Trim();
                if (!string.IsNullOrEmpty(str))
                    results.Add(str);
            }

            if (label.ChildCount > 0)
                foreach (var c in label.Children.Select(child => FlattenElement(child)))
                    results.AddRange(c);

            return results;
        }

        public Sextant? GetMouseoverSextant()
        {
            Sextant? result = null;
            var uiHover = GameController?.Game?.IngameState?.UIHover;
            if (uiHover?.Address == 0)
                return null;

            var inventoryItemIcon = uiHover?.AsObject<HoverItemIcon>();

            if (inventoryItemIcon == null || inventoryItemIcon.Tooltip == null || inventoryItemIcon.Tooltip.ChildCount == 0)
                return null;


            var lookupString = tryGetSextantString(inventoryItemIcon.Tooltip).Split('\n');
            if (lookupString.Length == 0)
                return null;

            return sextantRepo.GetSextant(lookupString);
        }


        internal string tryGetElementText(Element parent)
        {
            var result = "";
            var str = parent.GetText(512);
            if (!string.IsNullOrEmpty(str))
                result = str.Trim();

            return result;
        }

        internal string tryGetSextantString(Element parent)
        {
            var result = "";
            var root = parent.Children.FirstOrDefault();
            if (root != null && root.ChildCount >1)
            {
                var child = root.GetChildAtIndex(1);
                if (child.ChildCount > 2)
                {
                    var grandchild = child.GetChildAtIndex(3);

                    var str = grandchild.GetTextWithNoTags(512);
                    if (!string.IsNullOrEmpty(str))
                        result = str.Trim();
                    else result = "stat string null";
                }
                else result = "invalid grandchild count";
            }
            else result = "invalid child count";
            return result;
        }

    }
}
