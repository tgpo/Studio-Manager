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
using System.Windows.Media.Imaging;


namespace StudioManager
{
    // Define Global Constants
    public static class GlobalVar
    {
        public const string StudioFolder = @"C:\\Studio\\";
    }

    // Create data structure for ProjectItem class
    public class ProjectItem : INotifyPropertyChanged
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
        public int DisplayOrder { get; set; }
        public int Version { get; set; }

        // Watch for changes, and try to keep. Okay?
        // Property Changed Event Handler to raise update flag
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, e: new PropertyChangedEventArgs(propertyName));
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

        // When MainWindows gets <ProjectItems>, return our <ItemList> variable
        public ObservableCollection<ProjectItem> ProjectItems
        { get { return ItemList; } }

        // Declare <ItemList> as List of String
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
            var dirInfos = StudioFolder.GetDirectories("*.*").Where(x => (x.Attributes & FileAttributes.Hidden) == 0);

            // Run through <dirInfos> and populate <ProjectList>
            foreach (DirectoryInfo d in dirInfos)
            {
                ProjectList.Add(d.Name);
            }
        }


        // Method to populate our <ItemList> collection
        public void PopulateProjectItems(string startingFolder)
        {
            //Create Full Project Folder Path
            String FullFolderPath = GlobalVar.StudioFolder + startingFolder;

            // Clear out <ItemList>
            ItemList.Clear();

            // Set the Project folder location
            DirectoryInfo ProjectFolder = new DirectoryInfo(FullFolderPath);

            // Run through <ProjectFolder> and populate <ItemList>
            foreach (var file in ProjectFolder.GetFiles("*.jpg").Concat(ProjectFolder.GetFiles("*.png")))
            {
                // Create new BitmapImage from image file.
                // This allows us to delete the image without getting file in use errors.
                BitmapImage newImage = null;
                newImage = new BitmapImage();
                newImage.BeginInit();
                newImage.StreamSource = new FileStream(FullFolderPath + "\\" + file.Name, FileMode.Open, FileAccess.Read);
                newImage.CacheOption = BitmapCacheOption.OnLoad;
                newImage.EndInit();
                newImage.StreamSource.Dispose();

                // Set <ItemTitle> to file.Name with extention removed
                String FileName = file.Name;
                String ItemTitle = FileName.Remove(FileName.LastIndexOf('.'));

                // If Filename contains square brackets, use contents as <ItemTitle>
                if (ItemTitle.IndexOf('[') > -1)
                {
                    ItemTitle = ItemTitle.Split(new char[] { '[', ']' })[1];
                }

                // Add itemdetails to <ItemList>
                ItemList.Add(new ProjectItem { Title = ItemTitle, Comment = "First Comment", Image = newImage, ImageFileName = FullFolderPath + "\\" + file.Name, Version = 2, DisplayOrder = 2 });
            }

        }

        // Method called with Combobox selection is changed
        void ComboBox_Selectionchanged(object sender, SelectionChangedEventArgs e)
        {
            // Call our PopulateProjectItems method with the newly selected Project
            PopulateProjectItems( SelectedProject.SelectedItem.ToString() );
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
            String CurrentFileName = CurrentFilePath.Substring(CurrentFilePath.LastIndexOf(@"\") + 1);
                CurrentFileName = CurrentFileName.Substring(0, CurrentFileName.LastIndexOf("."));
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
                ItemList.First(d => d.Title == ItemToRename.Title).Title = UserFileName;
                
                
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

                    String DroppedFileName = file.Substring(file.LastIndexOf(@"\") + 1);
                    String DroppedTitle = DroppedFileName.Substring(0, DroppedFileName.LastIndexOf(@"."));

                    String CurrentProject = SelectedProject.SelectedItem.ToString() + @"\";

                    BitmapImage newImage = null;
                    newImage = new BitmapImage();
                    newImage.BeginInit();
                    newImage.StreamSource = new FileStream(file, FileMode.Open, FileAccess.Read);
                    newImage.CacheOption = BitmapCacheOption.OnLoad;
                    newImage.EndInit();
                    newImage.StreamSource.Dispose();


                    //Copy File to Studio Folder for current Project
                    System.IO.File.Copy(file, GlobalVar.StudioFolder + CurrentProject + DroppedFileName, true);

                    // Insert the item. 
                    ItemList.Add(new ProjectItem { Title = DroppedTitle, Comment = "Test Comment", Image = newImage, ImageFileName = GlobalVar.StudioFolder + CurrentProject + DroppedFileName, Version = 2, DisplayOrder = 2 });
                }
            }
        }


    }
}
