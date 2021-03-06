﻿using SolidWorks.Interop.sldworks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AngelSix.SolidDna
{
    /// <summary>
    /// A command group for the top command bar in SolidWorks
    /// </summary>
    public class CommandManagerGroup : SolidDnaObject<ICommandGroup>
    {
        #region Private Members

        /// <summary>
        /// Keeps track if this group has been created already
        /// </summary>
        private bool mCreated;

        /// <summary>
        /// A dictionary with all icon sizes and their paths.
        /// Entries are only added when path exists.
        /// </summary>
        private readonly Dictionary<int, string> mIconListPaths = new Dictionary<int, string>();

        /// <summary>
        /// List of icon sizes used by SOLIDWORKS. Icons are square, so these values are both width and height.
        /// </summary>
        private readonly int[] mIconSizes = {20, 32, 40, 64, 96, 128};

        /// <summary>
        /// A list of all tabs that have been created
        /// </summary>
        private readonly Dictionary<CommandManagerTabKey, CommandManagerTab> mTabs = new Dictionary<CommandManagerTabKey, CommandManagerTab>();

        #endregion

        #region Public Properties

        /// <summary>
        /// The Id used when this command group was created
        /// </summary>
        public int UserId { get; }

        /// <summary>
        /// The title of this command group
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The hint of this command group
        /// </summary>
        public string Hint { get; set; }

        /// <summary>
        /// The tooltip of this command group
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// The type of documents to show this command group in as a menu
        /// </summary>
        public ModelTemplateType MenuVisibleInDocumentTypes
        {
            get => (ModelTemplateType)mBaseObject.ShowInDocumentType;
            set =>
                // Set base object
                mBaseObject.ShowInDocumentType = (int)value;
        }

        /// <summary>
        /// Whether this command group has a Menu.
        /// NOTE: The menu is the regular drop-down menu like File, Edit, View etc...
        /// </summary>
        public bool HasMenu
        {
            get => mBaseObject.HasMenu;
            set =>
                // Set base object
                mBaseObject.HasMenu = value;
        }

        /// <summary>
        /// Whether this command group has a Toolbar.
        /// NOTE: The toolbar is the small icons like the top-left SolidWorks menu New, Save, Open etc...
        /// </summary>
        public bool HasToolbar
        {
            get => mBaseObject.HasToolbar;
            set => mBaseObject.HasToolbar = value;
        }

        /// <summary>
        /// The command items to add to this group
        /// </summary>
        public List<CommandManagerItem> Items { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="commandGroup">The SolidWorks command group</param>
        /// <param name="items">The command items to add</param>
        /// <param name="userId">The unique Id this group was assigned with when created</param>
        /// <param name="title">The title</param>
        /// <param name="hint">The hint</param>
        /// <param name="tooltip">The tool tip</param>
        /// <param name="hasMenu">Whether the CommandGroup should appear in the Tools dropdown menu.</param>
        /// <param name="hasToolbar">Whether the CommandGroup should appear in the Command Manager and as a separate toolbar.</param>
        public CommandManagerGroup(ICommandGroup commandGroup, List<CommandManagerItem> items, int userId, string title, string hint, string tooltip, bool hasMenu, bool hasToolbar) : base(commandGroup)
        {
            // Store user Id, used to remove the command group
            UserId = userId;

            // Set items
            Items = items;

            // Set title
            Title = title;

            // Set hint
            Hint = hint;

            // Set tooltip
            Tooltip = tooltip;

            // Set defaults

            // Show for all types
            MenuVisibleInDocumentTypes = ModelTemplateType.Assembly | ModelTemplateType.Part | ModelTemplateType.Drawing;

            // Have a menu
            HasMenu = hasMenu;

            // Have a toolbar
            HasToolbar = hasToolbar;

            // Listen out for callbacks
            PlugInIntegration.CallbackFired += PlugInIntegration_CallbackFired;
        }

        #endregion

        #region Icon List Methods

        /// <summary>
        /// The list of full paths to a bmp or png's that contains the icon list 
        /// from first in the list being the smallest, to last being the largest
        /// NOTE: Supported sizes for each icon in an array is 20x20, 32x32, 40x40, 64x64, 96x96 and 128x128
        /// </summary>
        public string[] GetIconListPaths()
        {
            return mIconListPaths.Values.ToArray();
        }

        /// <summary>
        /// Sets all icon lists based on a string format of the absolute path to the icon list images, replacing {0} with the size.
        /// For example C:\Folder\myiconlist{0}.png would look for all sizes such as
        /// C:\Folder\myiconlist20.png
        /// C:\Folder\myiconlist32.png
        /// C:\Folder\myiconlist40.png
        /// ... and so on
        /// </summary>
        /// <param name="pathFormat">The absolute path, with {0} used to replace with the icon size</param>
        public void SetIconLists(string pathFormat)
        {
            // Make sure we have something
            if (string.IsNullOrWhiteSpace(pathFormat))
                return;

            // Make sure the path format contains "{0}"
            if (!pathFormat.Contains("{0}"))
                throw new SolidDnaException(SolidDnaErrors.CreateError(
                    SolidDnaErrorTypeCode.SolidWorksCommandManager,
                    SolidDnaErrorCode.SolidWorksCommandGroupInvalidPathFormatError,
                    Localization.GetString("ErrorSolidWorksCommandGroupIconListInvalidPathError")));


            // Fill the dictionary with all paths that exist
            foreach (var iconSize in mIconSizes)
            {
                var path = string.Format(pathFormat, iconSize);
                if (File.Exists(path))
                {
                    mIconListPaths.Add(iconSize, path);
                }
            }
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Fired when a SolidWorks callback is fired
        /// </summary>
        /// <param name="name">The name of the callback that was fired</param>
        private void PlugInIntegration_CallbackFired(string name)
        {
            // Find the item, if any
            var item = Items.FirstOrDefault(f => f.CallbackId == name);

            // Call the action
            item?.OnClick?.Invoke();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a command item to the group
        /// </summary>
        /// <param name="item">The item to add</param>
        public void AddCommandItem(CommandManagerItem item)
        {
            // Add the item
            var id = mBaseObject.AddCommandItem2(item.Name, item.Position, item.Hint, item.Tooltip, item.ImageIndex, $"Callback({item.CallbackId})", null, UserId, (int)item.ItemType);

            // Set the Id we got
            item.UniqueId = id;
        }

        /// <summary>
        /// Creates the command group based on its current children
        /// NOTE: Once created, parent command manager must remove and re-create the group
        /// This group cannot be re-used after creating, any edits will not take place
        /// </summary>
        /// <param name="manager">The command manager that is our owner</param>
        public void Create(CommandManager manager)
        {
            if (mCreated)
                throw new SolidDnaException(SolidDnaErrors.CreateError(
                    SolidDnaErrorTypeCode.SolidWorksCommandManager,
                    SolidDnaErrorCode.SolidWorksCommandGroupReActivateError,
                    Localization.GetString("ErrorSolidWorksCommandGroupReCreateError")));

            #region Set Icons

            //
            // Set the icons
            //
            // NOTE: The order in which you specify the icons must be the same for this property and MainIconList.
            //
            //       For example, if you specify an array of paths to 
            //       20 x 20 pixels, 32 x 32 pixels, and 40 x 40 pixels icons for this property, 
            //       then you must specify an array of paths to 
            //       20 x 20 pixels, 32 x 32 pixels, and 40 x 40 pixels icons for MainIconList.
            //

            // Set all icon lists 
            var icons = GetIconListPaths();

            // 2016+ support
            mBaseObject.IconList = icons;

            // <2016 support
            if (icons.Length > 0)
            {
                // Largest icon for this one
                mBaseObject.LargeIconList = icons.Last();

                // The list of icons
                mBaseObject.MainIconList = icons;

                // Smallest icon for this one
                mBaseObject.SmallIconList = icons.First();
            }

            #endregion

            #region Add Items

            // Add items
            Items?.ForEach(AddCommandItem);

            #endregion

            // Activate the command group
            mCreated = mBaseObject.Activate();

            // Get command Ids
            Items?.ForEach(item => item.CommandId = mBaseObject.CommandID[item.UniqueId]);

            #region Command Tab

            // Add to parts tab
            var list = Items?.Where(f => f.TabView != CommandManagerItemTabView.None && f.VisibleForParts).ToList();
            if (list?.Count > 0)
                AddItemsToTab(ModelType.Part, manager, list);

            // Add to assembly tab
            list = Items?.Where(f => f.TabView != CommandManagerItemTabView.None && f.VisibleForAssemblies).ToList();
            if (list?.Count > 0)
                AddItemsToTab(ModelType.Assembly, manager, list);

            // Add to drawing tab
            list = Items?.Where(f => f.TabView != CommandManagerItemTabView.None && f.VisibleForDrawings).ToList();
            if (list?.Count > 0)
                AddItemsToTab(ModelType.Drawing, manager, list);

            #endregion

            // If we failed to create, throw
            if (!mCreated)
                throw new SolidDnaException(SolidDnaErrors.CreateError(
                    SolidDnaErrorTypeCode.SolidWorksCommandManager,
                    SolidDnaErrorCode.SolidWorksCommandGroupActivateError,
                    Localization.GetString("ErrorSolidWorksCommandGroupActivateError")));
        }

        /// <summary>
        /// Adds all <see cref="Items"/> to the command tab of the given title and model type
        /// </summary>
        /// <param name="type">The tab for this type of model</param>
        /// <param name="manager">The command manager</param>
        /// <param name="items">Items to add</param>
        /// <param name="title">The title of the tab</param>
        public void AddItemsToTab(ModelType type, CommandManager manager, List<CommandManagerItem> items, string title = "")
        {
            // Use default title if not specified
            if (string.IsNullOrEmpty(title))
                title = Title;

            CommandManagerTab tab;

            // Get the tab if it already exists
            if (mTabs.Any(f => string.Equals(f.Key.Title, title) && f.Key.ModelType == type))
                tab = mTabs.First(f => string.Equals(f.Key.Title, title) && f.Key.ModelType == type).Value;
            // Otherwise create it
            else
            {
                // Get the tab
                tab = manager.GetCommandTab(type, title, createIfNotExist: true);

                // Keep track of this tab
                mTabs.Add(new CommandManagerTabKey { ModelType = type, Title = title }, tab);
            }

            // New list of values
            var ids = new List<int>();
            var styles = new List<int>();

            // Add each items Id and style
            items.ForEach(item =>
            {
                // Add command Id
                ids.Add(item.CommandId);

                // Add style
                styles.Add((int)item.TabView);
            });

            // Add all the items
            tab.Box.UnsafeObject.AddCommands(ids.ToArray(), styles.ToArray());
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposing
        /// </summary>
        public override void Dispose()
        {
            // Stop listening out for callbacks
            PlugInIntegration.CallbackFired -= PlugInIntegration_CallbackFired;

            // Dispose all tabs
            foreach (var tab in mTabs.Values)
                tab.Dispose();

            base.Dispose();
        }

        #endregion
    }
}
