using System;
using System.Collections.Generic;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;

namespace UnityEditor.ProBuilder.UI
{
    class ToolbarMenuItem : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<ToolbarMenuItem, UxmlTraits> { }
        public MenuAction action;
        public bool iconMode;

        public void RefreshContents()
        {
            var state = action.menuActionState;
            var valid = iconMode ? SetupIcon(this, action) : SetupText(this, action);

            style.display = (state & MenuAction.MenuActionState.Visible) == MenuAction.MenuActionState.Visible && valid
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            SetEnabled((state & MenuAction.MenuActionState.Enabled) == MenuAction.MenuActionState.Enabled);

            var options = this.Q<VisualElement>("Options");

            if (action.optionsVisible)
            {
                options.style.display = DisplayStyle.Flex;
                options.SetEnabled(action.optionsEnabled);
            }
            else
            {
                options.style.display = DisplayStyle.None;
            }
        }

        static bool SetupIcon(VisualElement ui, MenuAction action)
        {
            if (action.icon == null)
                return false;
            var color = ToolbarGroupUtility.GetColor(action.group);
            var button = ui.Q<Button>("Button");
            button.style.borderLeftColor = color;
            button.tooltip = action.tooltip.summary;
            button.iconImage = action.icon;
            // todo context click opens options
            return true;
        }

        static bool SetupText(VisualElement ui, MenuAction action)
        {
            var color = ToolbarGroupUtility.GetColor(action.group);

            var button = ui.Q<Button>("Button");
            var label = button.Q<Label>("Label");
            var swatch = ui.Q<VisualElement>("CategorySwatch");
            var options = ui.Q<Button>("Options");

            swatch.style.backgroundColor = color;
            button.tooltip = action.tooltip.summary;
            label.text = action.menuTitle;

            options.style.borderLeftColor = color;
            options.style.borderRightColor = color;
            options.style.borderBottomColor = color;
            options.style.borderTopColor = color;

            return true;
        }
    }

    class ProBuilderToolbar : VisualElement
    {
        const string k_IconMode = "ToolbarIcon";
        const string k_TextMode = "ToolbarLabel";
        const string k_UI = "Packages/com.unity.probuilder/Content/UI";

        readonly List<ToolbarMenuItem> m_Actions = new List<ToolbarMenuItem>();

        public ProBuilderToolbar()
        {
            CreateGUI();

            ProBuilderEditor.selectModeChanged += RefreshVisibility;
            MeshSelection.objectSelectionChanged += RefreshVisibility;
            ProBuilderMesh.elementSelectionChanged += RefreshVisibility;

            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                ProBuilderEditor.selectModeChanged -= RefreshVisibility;
                MeshSelection.objectSelectionChanged -= RefreshVisibility;
                ProBuilderMesh.elementSelectionChanged -= RefreshVisibility;
            });
        }

        void RefreshVisibility(ProBuilderMesh obj) => RefreshVisibility();

        void RefreshVisibility(SelectMode obj) => RefreshVisibility();

        void RefreshVisibility()
        {
            foreach (var element in m_Actions)
                element.RefreshContents();
        }

        public void CreateGUI()
        {
            m_Actions.Clear();

            var iconMode = ProBuilderEditor.s_IsIconGui;
            var menuContentAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{k_UI}/{(iconMode ? k_IconMode : k_TextMode)}.uxml");
            var actions = EditorToolbarLoader.GetActions(true);

            VisualElement scrollContentsRoot = new ScrollView(ScrollViewMode.Vertical);
            Add(scrollContentsRoot);

            if (iconMode)
            {
                var container = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{k_UI}/ToolbarIconContainer.uxml");
                var contents = container.Instantiate();
                scrollContentsRoot.Add(contents);
                scrollContentsRoot = contents.Q<VisualElement>("IconRoot");
            }

            for(int i = 0, c = actions.Count; i < c; ++i)
            {
                var menu = menuContentAsset.Instantiate().Q<ToolbarMenuItem>();
                var action = actions[i];

                menu.iconMode = iconMode;
                menu.action = action;
                action.changed += menu.RefreshContents;
                action.RegisterChangedCallbacks();
                menu.RegisterCallback<DetachFromPanelEvent>(_ => action.UnregisterChangedCallbacks());
                menu.RefreshContents();

                var button = menu.Q<Button>("Button");
                button.clicked += () => action.PerformAction();

                if (!iconMode)
                {
                    var options = menu.Q<Button>("Options");
                    options.clicked += action.PerformAltAction;
                }

                m_Actions.Add(menu);
                scrollContentsRoot.Add(menu);
            }

            RefreshVisibility();
        }

    }

    class Toolbar2 : EditorWindow
    {
        ProBuilderToolbar m_Toolbar;

        [MenuItem("Window/Toolbar 2")]
        static void init() => GetWindow<Toolbar2>();

        public void CreateGUI()
        {
            rootVisualElement.Add(new Button(() =>
            {
                var toolbar = rootVisualElement.Q<ProBuilderToolbar>();
                if (toolbar != null)
                    rootVisualElement.Remove(toolbar);
                rootVisualElement.Add(new ProBuilderToolbar());
            }) { text = "Rebuild" });
            rootVisualElement.Add(new ProBuilderToolbar());
        }
    }
}
