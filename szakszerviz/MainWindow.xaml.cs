using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace szakszerviz
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int SelectedId { get; set; }
        private string SaveDataText { get; set; }
        private string EditDataText { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            SaveDataText = "Adatok felvitele";
            EditDataText = "Adatok módosítása";
            selectEditSaveMethod(false);


            loadComboBoxes();
            resetInputs();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadDataGrid(dg_szerviz);
        }


        private void delete_btn_Click(object sender, RoutedEventArgs e)
        {

            if(dg_szerviz.SelectedIndex != -1 && SelectedId > 0)
            {

               var delete = MessageBox.Show("Biztosan törölni szeretnéd a kijelölt tételt?","Figyelem!",MessageBoxButton.YesNo,MessageBoxImage.Warning);

                if(delete == MessageBoxResult.Yes)
                {
                    deleteService();
                }

            }

        }

        private void reset_btn_Click(object sender, RoutedEventArgs e)
        {

            selectEditSaveMethod(false);

        }

        private void save_btn_Click(object sender, RoutedEventArgs e)
        {
            var rendszam = inp_rendszam.Text.ToUpper();
            var marka = inp_marka.Text;
            var tipus = inp_tipus.Text;
            var forgalomban = inp_forg;
            var szolgaltatas = inp_szolg;

            var szolgaltatasId = getSzolgaltatasId(szolgaltatas.SelectedItem.ToString());


            if(string.IsNullOrWhiteSpace(rendszam) || string.IsNullOrWhiteSpace(marka) || string.IsNullOrWhiteSpace(tipus) || forgalomban.SelectedDate is null || szolgaltatas.SelectedItem is null)
            {
                MessageBox.Show("Minden mező kitöltése kötelező!");
            }
            else
            {

                if(SelectedId > 0)
                {
                    var edit = MessageBox.Show("Biztosan módosítani szeretnéd a kijelölt tételt?", "Figyelem!", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (edit == MessageBoxResult.Yes)
                    {
                        updateService(rendszam, marka, tipus, forgalomban.SelectedDate.Value, szolgaltatasId);
                    }
                }
                else
                {
                   addNewService(rendszam, marka, tipus, forgalomban.SelectedDate.Value, szolgaltatasId);
                }

                loadDataGrid(dg_szerviz);
                selectEditSaveMethod(false);
            }
        }


        private void export_btn_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new SaveFileDialog();
            dialog.Filter = "Comma-separated values file (*.csv)|*.csv";
            dialog.ShowDialog();

            if (radio_osszes.IsChecked.Value && dialog.FileName.Length > 0)
            {
                exportAllService(dialog.FileName.ToString());
            }
            else
            {
                exportServiceBy(inp_szolg_export.SelectedItem.ToString(), dialog.FileName.ToString());
            }
        }

        private void exportAllService(string exportPath)
        {
            using (var db = new SzakszervizDBEntities())
            {
                var export = db.szerviz.Include("szolgaltatas").ToList();

                using (var fs = new FileStream(exportPath,FileMode.CreateNew))
                {
                    using (var sw = new StreamWriter(fs,Encoding.UTF8))
                    {
                        foreach (var s in export)
                        {
                            var line = $"{s.rendszam};{s.marka};{s.tipus};{s.forgalomban};{s.szolgaltatas.nev};{s.felvetel}";
                            sw.WriteLine(line);
                        }
                    }
                }

                MessageBox.Show("A mentés megtörtént!");

            }
        }

        private void exportServiceBy(string szolgaltatas, string exportPath)
        {
            using (var db = new SzakszervizDBEntities())
            {
                var szolgId = getSzolgaltatasId(szolgaltatas);
                var export = db.szerviz.Include("szolgaltatas").Where(x=>x.fk_szolgaltatas_id==szolgId).ToList();
                using (var fs = new FileStream(exportPath, FileMode.CreateNew))
                {
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        foreach (var s in export)
                        {
                            var line = $"{s.szolgaltatas.nev};{s.rendszam};{s.marka};{s.tipus}";
                            sw.WriteLine(line);
                        }
                    }
                }

                MessageBox.Show("A mentés megtörtént!");
            }
        }



        private void kereses_btn_Click(object sender, RoutedEventArgs e)
        {
            var keres = inp_kereses.Text;
            if (string.IsNullOrWhiteSpace(keres))
            {
                MessageBox.Show("Nem adtál meg keresési értéket!");
                return;
            }
            else
            {
                searchInDb(keres, dg_szerviz);
            }

        }



        private void vissza_btn_Click(object sender, RoutedEventArgs e)
        {
            inp_kereses.Clear();
            loadDataGrid(dg_szerviz);

        }

        private void dg_szerviz_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

            if(dg_szerviz.SelectedIndex != -1)
            {
                selectEditSaveMethod(true);

                var selected = (szerviz)dg_szerviz.SelectedItem;
                SelectedId = selected.Id;
                Console.WriteLine(SelectedId);

                inp_rendszam.Text = selected.rendszam;
                inp_marka.Text = selected.marka;
                inp_tipus.Text = selected.tipus;
                inp_forg.SelectedDate = selected.forgalomban;
                inp_szolg.SelectedItem = selected.szolgaltatas.nev;

            }
       
        }



        /// Methods
        /// 


        private void loadComboBoxes()
        {
            using (var db = new SzakszervizDBEntities())
            {
                var list = db.szolgaltatas.ToList();
                list.ForEach(x =>
                {
                    inp_szolg.Items.Add(x.nev);
                    inp_szolg_export.Items.Add(x.nev);
                });
             
            }
        }

        private int getSzolgaltatasId(string nev)
        {
            using (var db = new SzakszervizDBEntities())
            {
                return db.szolgaltatas.FirstOrDefault(x => x.nev == nev).Id;
            }
        }


        private void loadDataGrid(DataGrid dg)
        {
            using (var db = new SzakszervizDBEntities())
            {
                dg.ItemsSource = db.szerviz.Include("szolgaltatas").OrderBy(x=>x.felvetel).ToList();
                dg.Items.Refresh();
            }
        }

        private void searchInDb(string rendszam, DataGrid dg)
        {
            using (var db = new SzakszervizDBEntities())
            {
                var keres = db.szerviz.Where(x => x.rendszam.Contains(rendszam));
                if(keres.Count()>0) dg.ItemsSource = keres.Include("szolgaltatas").ToList();
                else
                {
                    MessageBox.Show("Nincs találat!");
                }

            }
        }

        private void resetInputs()
        {
            inp_rendszam.Clear();
            inp_marka.Clear();
            inp_tipus.Clear();
            inp_szolg.SelectedIndex = 0;
            inp_forg.SelectedDate = DateTime.Now;
            SelectedId = 0;

        }

        private void selectEditSaveMethod(bool edit)
        {
            resetInputs();

            if (edit)
            {
                input_label_text.Content = EditDataText;
                save_btn.Content = EditDataText;
                delete_btn.IsEnabled = true;
                reset_btn.IsEnabled = true;
            }
            else
            {
                input_label_text.Content = SaveDataText;
                save_btn.Content = SaveDataText;
                delete_btn.IsEnabled = false;
                reset_btn.IsEnabled = false;

            }

        }


        ///// Database methods
        ///
        private void addNewService(string rendszam, string marka, string tipus, DateTime forgalomban, int szolgaltatasId)
        {
            using (var db = new SzakszervizDBEntities())
            {

                var newItem = new szerviz()
                {
                    rendszam = rendszam,
                    marka = marka,
                    tipus = tipus,
                    forgalomban = forgalomban,
                    fk_szolgaltatas_id = szolgaltatasId,
                    felvetel = DateTime.Now
                };
                db.szerviz.Add(newItem);
                db.SaveChanges();
                MessageBox.Show("Sikeresen hozzáadtad a tételt!");

            }

        }

        private void updateService(string rendszam, string marka, string tipus, DateTime forgalomban, int szolgaltatasId)
        {
            using (var db = new SzakszervizDBEntities())
            {

                var edit = db.szerviz.FirstOrDefault(x => x.Id == SelectedId);

                edit.rendszam = rendszam;
                edit.marka = marka;
                edit.tipus = tipus;
                edit.forgalomban = forgalomban;
                edit.fk_szolgaltatas_id = szolgaltatasId;
                edit.felvetel = DateTime.Now;
                db.SaveChanges();

                MessageBox.Show("Sikeresen módosítottad a tételt!");

            }

        }

        private void deleteService()
        {
            using (var db = new SzakszervizDBEntities())
            {
                var range = db.szerviz.Where(x => x.Id == SelectedId);
                db.szerviz.RemoveRange(range);
                db.SaveChanges();
                MessageBox.Show("Sikeresen törölted a tételt!");
                loadDataGrid(dg_szerviz);
            }

        }



    }
}
