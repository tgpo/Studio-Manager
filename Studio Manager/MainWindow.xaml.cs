using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace StudioManager
{
    // Define Global Constants
    public static class GlobalVar
    {
        public const string StudioFolder = @"C:\\Studio\\";
    }

    // Create data structure for ProjectItem class
    public class ProjectInfo
    {
        public String Name { get; set; }
        public String Parent { get; set; }
        public DirectoryInfo[] SubProjects { get; set; }
    }

    // Create data structure for ProjectItem class
    public class ProjectItem : INotifyPropertyChanged, IComparable
    {
        public BitmapImage Image { get; set; }
        private string blankImageFileName;

        public string ImageFileName
        {
            get
            {
                return blankImageFileName;
            }

            set
            {
                if (value != blankImageFileName)
                {
                    blankImageFileName = value;
                    RaisePropertyChanged("ImageFileName");
                }
            }
        }

        private string blankTitle;

        public string Title
        {
            get
            {
                return blankTitle;
            }

            set
            {
                if (value != blankTitle)
                {
                    blankTitle = value;
                    RaisePropertyChanged("Title");
                }
            }
        }
        public string Comment { get; set; }

        private int blankDisplayOrder;
        public int DisplayOrder
        {
            get
            {
                return blankDisplayOrder;
            }

            set
            {
                if (value != blankDisplayOrder)
                {
                    blankDisplayOrder = value;
                    RaisePropertyChanged("DisplayOrder");
                }
            }
        }

        private int blankItemVersion;
        public int Version
        {
            get
            {
                return blankItemVersion;
            }

            set
            {
                if (value != blankItemVersion)
                {
                    blankItemVersion = value;
                    RaisePropertyChanged("Version");
                }
            }
        }

        public int CompareTo(object obj)
        {
            ProjectItem item = obj as ProjectItem;
            if (item == null)
            {
                throw new ArgumentException("Object is not ProjectItem");
            }
            return this.DisplayOrder.CompareTo(item.DisplayOrder);
        }

        // Watch for changes, and try to keep. Okay?
        // Property Changed Event Handler to raise update flag
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, e: new PropertyChangedEventArgs(propertyName));
        }


    }

    public static class ListExtension
    {
        public static void BubbleSort(this System.Collections.IList o)
        {
            for (int i = o.Count - 1; i >= 0; i--)
            {
                for (int j = 1; j <= i; j++)
                {
                    object o1 = o[j - 1];
                    object o2 = o[j];
                    if (((IComparable)o1).CompareTo(o2) > 0)
                    {
                        o.Remove(o1);
                        o.Insert(j, o1);
                    }
                }
            }
        }
    }

    // Define Custom commands used in XAML
    public static class Command
    {
        public static readonly RoutedUICommand Rename = new RoutedUICommand("Rename Filename", "RenameFile", typeof(MainWindow));
        public static readonly RoutedUICommand CreateProject = new RoutedUICommand("Create New Project", "CreateProject", typeof(MainWindow));
    }

    // Interaction logic for MainWindow.xaml
    public partial class MainWindow : Window
    {

        // Vars:
        // <ItemList> - Collection of items for current project
        // <ProjectItems> - Public collection that returns <ItemList>. Used for ItemSource binding.

        
        // Declare <ItemList> as ObservableCollection of ProjectItem
        ObservableCollection<ProjectItem> ItemList = new ObservableCollection<ProjectItem>();

        // Declare <ProjectLookupList> as List of ProjectInfo
        List<ProjectInfo> ProjectLookupList = new List<ProjectInfo>();

        // When MainWindows gets <ProjectItems>, return our <ItemList> variable
        public ObservableCollection<ProjectItem> ProjectItems
        { get { return ItemList; } }

        // Declare <ProjectList> as List of String
        public List<String> ProjectList = new List<String>();

        // When MainWindows gets <Projects>, return our <ProjectList> variable
        public List<String> Projects
        { get { return ProjectList; } }


        // Window Loaded...Now what?
        public MainWindow()
        {
            InitializeComponent();

            PopulateProjectList();
            PopulateProjectItems("");

        }

        public void PopulateProjectList()
        {
            // Clear out <ProjectList>
            ProjectList.Clear();

            // Set the Studio folder location
            DirectoryInfo StudioFolder = new DirectoryInfo(GlobalVar.StudioFolder);

            // Get the visible subdirectories under <StudioFolder>
            var dirInfos = StudioFolder.GetDirectories("*.*", SearchOption.AllDirectories).Where(x => (x.Attributes & FileAttributes.Hidden) == 0);

            // Run through <dirInfos> and populate <ProjectList>
            foreach (DirectoryInfo d in dirInfos)
            {
                ProjectList.Add(d.Name);
                DirectoryInfo[] subdirs = d.GetDirectories();

                if (subdirs.Length != 0)
                {
                    ProjectLookupList.Add(new ProjectInfo { Name = d.Name, Parent = d.Parent.Name, SubProjects = subdirs });

                    //We only need to go one level deep in subfolders
                    foreach (DirectoryInfo sd in subdirs)
                    {
                        ProjectList.Add("- " + sd.Name);
                    }
                }
                else
                {
                    ProjectLookupList.Add(new ProjectInfo { Name = d.Name, Parent = d.Parent.Name, SubProjects = null });
                }
            }
        }


        // Method to populate our <ItemList> collection
        public void PopulateProjectItems(string startingFolder)
        {

            String FullFolderPath;

            // If Parent Exists, add it to startingFolder Directory!
            var thisFolder = ProjectLookupList.Find(item => item.Name == startingFolder);
            if (checkNotNull(thisFolder) && checkNotStudio(thisFolder.Parent))
            {
               FullFolderPath  = GlobalVar.StudioFolder + thisFolder.Parent + @"\" + startingFolder;
            }
            else
            {
                //Create Full Project Folder Path
                FullFolderPath = GlobalVar.StudioFolder + startingFolder;
            }

            // Clear out <ItemList>
            ItemList.Clear();

            // Set the Project folder location
            DirectoryInfo ProjectFolder = new DirectoryInfo(FullFolderPath);

            // Run through <ProjectFolder> and populate <ItemList>
            foreach (var file in ProjectFolder.GetFiles("*.jpg").Concat(ProjectFolder.GetFiles("*.png")))
            {
                // Create new BitmapImage from image file.
                // This allows us to delete the image without getting file in use errors.
                BitmapImage newImage = createNewBitmap(FullFolderPath + "\\" + file.Name);

                // Set <ItemTitle> based on file.Name
                String ItemTitle = getItemTitle(file.Name);

                // Set <ItemDisplayOrder> based on file.Name
                int ItemDisplayOrder = getItemDisplayOrder(file.Name);

                // Set <ItemDisplayOrder> based on file.Name
                int ItemVersion = getItemVersion(file.Name);
                

                // Add itemdetails to <ItemList>
                ItemList.Add(new ProjectItem { Title = ItemTitle, Comment = "First Comment", Image = newImage, ImageFileName = FullFolderPath + "\\" + file.Name, Version = ItemVersion, DisplayOrder = ItemDisplayOrder });
                ItemList.BubbleSort();
            }

        }

        // Method called with Combobox selection is changed
        void ComboBox_Selectionchanged(object sender, SelectionChangedEventArgs e)
        {
            String SelectedProjectName = currentProjectName();

            // Call our PopulateProjectItems method with the newly selected Project
            PopulateProjectItems(SelectedProjectName);
        }


        // Method called when project item Delete button is pressed
        private void ItemDelete(object sender, ExecutedRoutedEventArgs e)
        {
            // Remove item from <ItemList>
            ProjectItem ItemToDelete = e.Parameter as ProjectItem;
            ItemList.Remove(ItemToDelete);

            // Delete the image file
            File.Delete( ItemToDelete.ImageFileName );
 
        }

        // Method called by Rename button
        // Renames ProjectItem Image to user specified filename
        private void RenameFile(object sender, ExecutedRoutedEventArgs e)
        {
            ProjectItem ItemToRename = e.Parameter as ProjectItem;

            // Create Needed variables based on Current FileName 
            String CurrentFilePath = ItemToRename.ImageFileName;
            String CurrentFileName = getFileName(CurrentFilePath, false);
            String CurrentProjectFolder = CurrentFilePath.Substring(0, CurrentFilePath.LastIndexOf(@"\")) + @"\";
            String FileExt = CurrentFilePath.Substring(CurrentFilePath.LastIndexOf("."));

            // Request User Input <NewFileName>
            String UserFileName = Microsoft.VisualBasic.Interaction.InputBox("What do you want to rename the file to?", "Rename " + CurrentFileName, CurrentFileName);
            
            // Only Rename if user entered something
            if (UserFileName.Length > 0)
            {
                String NewFileName = CurrentProjectFolder + UserFileName + FileExt;
                File.Move(CurrentFilePath, NewFileName);

                //Update Image Filename in <ItemList>
                ItemList.First(d => d.ImageFileName == ItemToRename.ImageFileName).ImageFileName = CurrentProjectFolder + UserFileName + FileExt;

                //Update Title in <ItemList>
                ItemList.First(d => d.ImageFileName == ItemToRename.ImageFileName).Title = getItemTitle(UserFileName + FileExt);

                //Update Version in <ItemList>
                ItemList.First(d => d.ImageFileName == ItemToRename.ImageFileName).Version = getItemVersion(UserFileName + FileExt);

                //Update DisplayOrder in <ItemList>
                ItemList.First(d => d.ImageFileName == ItemToRename.ImageFileName).DisplayOrder = getItemDisplayOrder(UserFileName + FileExt);

                ItemList.BubbleSort();
                
                
            }
        }

        private void CreateNewProject(object sender, RoutedEventArgs e)
        {
            // Specify the directory you want to manipulate. 
            string path = GlobalVar.StudioFolder + newProjectName.Text;

            // Determine whether the directory exists. 
            if (System.IO.Directory.Exists(path))
            {
                System.Windows.MessageBox.Show(newProjectName.Text + " Project Already Exists");

                return;
            }

            // Create the directory.
            System.IO.DirectoryInfo di = System.IO.Directory.CreateDirectory(path);

            System.Windows.MessageBox.Show(newProjectName.Text + " Project Created");

        }

        private void ItemsControl_Drop(object sender, System.Windows.DragEventArgs e)
        {

            String[] FileList = (String[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop, false);

            foreach (string file in FileList)
            {

                // Only accept PNG or JPG files
                if (Path.GetExtension(file).ToLower() == ".png" || Path.GetExtension(file).ToLower() == ".jpg")
                {

                    // Create variables for dropped file
                    String DroppedFileName = getFileName(file, true);
                    String DroppedTitle = getItemTitle(DroppedFileName);
                    int ItemDisplayOrder = getItemDisplayOrder(DroppedFileName);
                    int ItemVersion = getItemVersion(DroppedFileName);

                    String CurrentProject = currentProjectName();

                    // If Parent Exists, add it to startingFolder Directory!
                    var thisFolder = ProjectLookupList.Find(item => item.Name == CurrentProject);

                    if ( checkNotNull(thisFolder) && checkNotStudio(thisFolder.Parent))
                    {
                        CurrentProject = GlobalVar.StudioFolder + thisFolder.Parent + @"\" + CurrentProject + @"\";
                    }
                    else
                    {
                        //Create Full Project Folder Path
                        CurrentProject = GlobalVar.StudioFolder + CurrentProject + @"\";
                    }

                    BitmapImage newImage = createNewBitmap(file);

                    //Copy File to Studio Folder for current Project
                    System.IO.File.Copy(file, CurrentProject + DroppedFileName, true);

                    // Insert the item. 
                    ItemList.Add(new ProjectItem { Title = DroppedTitle, Comment = "Test Comment", Image = newImage, ImageFileName = CurrentProject + DroppedFileName, Version = ItemVersion, DisplayOrder = ItemDisplayOrder });
                    ItemList.BubbleSort();
                }
            }
        }

        private bool checkNotStudio(String thisFolder)
        {
            if (thisFolder != "Studio")
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private bool checkNotNull(ProjectInfo varToCheck)
        {
            if (varToCheck != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private BitmapImage createNewBitmap(String file)
        {
            BitmapImage newImage = null;
            newImage = new BitmapImage();
            newImage.BeginInit();
            newImage.StreamSource = new FileStream(file, FileMode.Open, FileAccess.Read);
            newImage.CacheOption = BitmapCacheOption.OnLoad;
            newImage.EndInit();
            newImage.StreamSource.Dispose();

            return newImage;
        }

        private String currentProjectName()
        {
            String CurrentProject = SelectedProject.SelectedItem.ToString();
            if (CurrentProject.IndexOf("-") == 0)
            {
                CurrentProject = CurrentProject.Substring(2);
            }

            return CurrentProject;
        }

        private String getItemTitle(String FileName)
        {
            String ItemTitle = FileName.Remove(FileName.LastIndexOf('.'));

            // If Filename contains square brackets, use contents as <ItemTitle>
            if (ItemTitle.IndexOf('[') > -1)
            {
                ItemTitle = ItemTitle.Split(new char[] { '[', ']' })[1];
            }

            return ItemTitle;
        }

        private String getFileName(String FileName, bool keepFileExt)
        {
            if (keepFileExt)
            {
                return FileName.Substring(FileName.LastIndexOf(@"\") + 1);
            } else {
                FileName = FileName.Substring(FileName.LastIndexOf(@"\") + 1);
                return FileName.Substring(0, FileName.LastIndexOf("."));
            }
            
        }

        private int getItemDisplayOrder(String FileName)
        {
            char[] chars = FileName.ToCharArray();
            int lastValid = -1;

            for (int i = 0; i < chars.Length; i++)
            {
                if (Char.IsDigit(chars[i]))
                {
                    lastValid = i;
                }
                else
                {
                    break;
                }
            }

            if (lastValid >= 0)
            {
                return int.Parse(new string(chars, 0, lastValid + 1));
            }
            else
            {
                return -1;
            }
        }

        private int getItemVersion(String FileName)
        {
            FileName = FileName.Remove(FileName.LastIndexOf('.'));

            if (FileName.IndexOf('.') > -1 && FileName.IndexOf('[') > -1)
            {
                return int.Parse(FileName.Split(new char[] { '.', '[' })[1]);
            }
            else
            {
                return -1;
            }

        }

    }
}
