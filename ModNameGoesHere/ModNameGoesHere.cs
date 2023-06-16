using System.Collections.Generic;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using BaseX;
using CodeX;
using FrooxEngine.LogiX;

namespace NeosDeadLogiXNodeFinder
{
    public class NeosDeadLogiXNodeFinder : NeosMod
    {
        public override string Name => "Dead LogiX Node Finder";
        public override string Author => "Nytra";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/Nytra/NeosComponentWizard";

        const string WIZARD_TITLE = "Dead LogiX Node Finder (Mod)";

        public override void OnEngineInit()
        {
            //Harmony harmony = new Harmony("owo.Nytra.ComponentSearchWizard");
            Engine.Current.RunPostInit(AddMenuOption);
        }
        void AddMenuOption()
        {
            DevCreateNewForm.AddAction("Editor", WIZARD_TITLE, (x) => ComponentSearchWizard.GetOrCreateWizard(x));
        }

        class ComponentSearchWizard
        {
            public static ComponentSearchWizard GetOrCreateWizard(Slot x)
            {
                return new ComponentSearchWizard(x);
            }

            Slot WizardSlot;

            readonly ReferenceField<Slot> processingRoot;

            readonly ReferenceMultiplexer<Component> results;

            readonly Button searchButton;

            // if the mod is currently performing an operation as a result of a button press
            bool performingOperations = false;

            readonly Text statusText;
            void UpdateStatusText(string info)
            {
                statusText.Content.Value = info;
            }

            bool ValidateWizard()
            {
                if (processingRoot.Reference.Target == null)
                {
                    UpdateStatusText("No search root provided!");
                    return false;
                }

                if (performingOperations)
                {
                    UpdateStatusText("Operations in progress! (Or the mod has crashed)");
                    return false;
                }

                return true;
            }

            ComponentSearchWizard(Slot x)
            {
                WizardSlot = x;
                WizardSlot.Tag = "Developer";
                WizardSlot.PersistentSelf = false;

                NeosCanvasPanel canvasPanel = WizardSlot.AttachComponent<NeosCanvasPanel>();
                canvasPanel.Panel.AddCloseButton();
                canvasPanel.Panel.AddParentButton();
                canvasPanel.Panel.Title = WIZARD_TITLE;
                canvasPanel.Canvas.Size.Value = new float2(800f, 816f);

                Slot Data = WizardSlot.AddSlot("Data");
                processingRoot = Data.AddSlot("processingRoot").AttachComponent<ReferenceField<Slot>>();
                processingRoot.Reference.Value = WizardSlot.World.RootSlot.ReferenceID;
                results = Data.AddSlot("referenceMultiplexer").AttachComponent<ReferenceMultiplexer<Component>>();

                UIBuilder UI = new UIBuilder(canvasPanel.Canvas);
                UI.Canvas.MarkDeveloper();
                UI.Canvas.AcceptPhysicalTouch.Value = false;

                UI.SplitHorizontally(0.5f, out RectTransform left, out RectTransform right);

                left.OffsetMax.Value = new float2(-2f);
                right.OffsetMin.Value = new float2(2f);

                UI.NestInto(left);

                VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
                verticalLayout.ForceExpandHeight.Value = false;

                UI.Style.MinHeight = 24f;
                UI.Style.PreferredHeight = 24f;
                UI.Style.PreferredWidth = 400f;
                UI.Style.MinWidth = 400f;

                UI.Text("Search Root:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
                UI.Next("Root");
                UI.Current.AttachComponent<RefEditor>().Setup(processingRoot.Reference);

                UI.Spacer(24f);

                searchButton = UI.Button("Search");
                searchButton.LocalPressed += SearchPressed;

                UI.Spacer(24f);

                UI.Text("Status:");
                statusText = UI.Text("---");

                UI.NestInto(right);
                UI.ScrollArea();
                UI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);

                SyncMemberEditorBuilder.Build(results.References, "DeadNodes", null, UI);

                WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
            }

            void SearchPressed(IButton button, ButtonEventData eventData)
            {
                if (!ValidateWizard()) return;

                performingOperations = true;
                searchButton.Enabled = false;

                results.References.Clear();

                int count = 0;
                bool stoppedEarly = false;

                foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren<LogixNode>(node => node.Enabled == false))
                {
                    if (results.References.Count >= 256)
                    {
                        stoppedEarly = true;
                        break;
                    }
                    count++;
                    results.References.Add(c);
                }

                if (stoppedEarly)
                {
                    UpdateStatusText($"Found {count} dead LogiX nodes (Max Results limit reached).");
                }
                else
                {
                    UpdateStatusText($"Found {count} dead LogiX nodes.");
                }

                performingOperations = false;
                searchButton.Enabled = true;
            }
        }
    }
}