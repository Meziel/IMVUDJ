using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IMVUDJ
{
	public class Preset : INotifyPropertyChanged
	{
		public Preset() { }

		public Preset(Preset workingPreset)
		{
			this.Name = workingPreset.Name;
			this.Parts = workingPreset.Parts;
			this.Delay = workingPreset.Delay;
		}

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
				this.OnPropertyChanged("Name");
			}
		}

		public int Parts
		{
			get
			{
				return parts;
			}
			set
			{
				parts = value;
				this.OnPropertyChanged("Parts");
			}
		}

		public int Delay
		{
			get
			{
				return delay;
			}
			set
			{
				delay = value;
				this.OnPropertyChanged("Delay");
			}
		}

		private string name;
		private int parts;
		private int delay;

		/// <summary>
		/// Notifies objects registered to receive this event that a property value has changed.
		/// </summary>
		/// <param name="propertyName">The name of the property that was changed.</param>
		protected virtual void OnPropertyChanged(string propertyName)
		{
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		[DllImport("user32.dll")]
		public static extern int SetForegroundWindow(IntPtr hWnd);

		public ObservableCollection<Preset> Presets { get; set; }
		public Preset WorkingPreset { get; set; } = new Preset();

		private bool stop = false;

		private const string DATA_SOURCE = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Michael\source\repos\IMVUDJ\IMVUDJ\Presets.mdf;Integrated Security=True";

		public MainWindow()
		{
			Presets = new ObservableCollection<Preset>();
			InitializeComponent();
			this.DataContext = this;
			PresetsList.ItemsSource = Presets;
			load();
		}

		private void Play(object sender, RoutedEventArgs e)
		{
			playSong(WorkingPreset.Name, WorkingPreset.Parts, WorkingPreset.Delay);
		}

		private void LoadPreset(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			Preset presetSelected = (Preset)(button.DataContext);
			WorkingPreset.Name = presetSelected.Name;
			WorkingPreset.Parts = presetSelected.Parts;
			WorkingPreset.Delay = presetSelected.Delay;
		}

		private void RemovePreset(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			Preset presetSelected = (Preset)(button.DataContext);

			using (SqlConnection connection = new SqlConnection(DATA_SOURCE))
			{
				SqlCommand command = new SqlCommand("DELETE FROM Presets WHERE Name = @Name;", connection);
				command.Parameters.AddWithValue("Name", presetSelected.Name);
				command.Connection.Open();
				command.ExecuteNonQuery();
			}

			Presets.Remove(presetSelected);
		}

		private void playSong(string text, int number, int delayMs)
		{
			IntPtr hWnd = IntPtr.Zero;
			foreach (Process pList in Process.GetProcesses())
			{
				if (pList.MainWindowTitle.Contains("IMVU"))
				{
					hWnd = pList.MainWindowHandle;
					break;
				}
			}

			if (hWnd == IntPtr.Zero)
			{
				return;
			}

			SetForegroundWindow(hWnd);

			new Thread(() =>
			{
				for (var i = 1; i <= number; i++)
				{
					sendMessage(text + i + '\n', hWnd);

					if (i < number)
					{
						Thread.Sleep(delayMs);
					}

					if (stop)
					{
						stop = false;
						return;
					}
				}
			}).Start();
		}

		private static void sendMessage(string message, IntPtr hWnd)
		{
			SendKeys.SendWait(message);
		}

		private void Stop(object sender, RoutedEventArgs e)
		{
			stop = true;
		}

		private void load()
		{
			using (SqlConnection connection = new SqlConnection(DATA_SOURCE))
			{
				SqlCommand command = new SqlCommand("SELECT Name, Parts, Delay FROM Presets", connection);
				command.Connection.Open();

				using (SqlDataReader reader = command.ExecuteReader())
				{
					while(reader.Read())
					{
						Preset preset = new Preset();
						preset.Name = reader.GetString(0);
						preset.Parts = reader.GetInt32(1);
						preset.Delay = reader.GetInt32(2);

						Presets.Add(preset);
					}
				}
			}
		}

		private void Save(object sender, RoutedEventArgs e)
		{
			Preset presetWithSameName = Presets.FirstOrDefault(preset => preset.Name == WorkingPreset.Name);
			if (presetWithSameName == null)
			{
				Presets.Add(new Preset(WorkingPreset));

				using (SqlConnection connection = new SqlConnection(DATA_SOURCE))
				{
					SqlCommand command = new SqlCommand("INSERT INTO Presets (Name, Parts, Delay) VALUES (@Name, @Parts, @Delay);", connection);
					command.Parameters.AddWithValue("Name", WorkingPreset.Name);
					command.Parameters.AddWithValue("Parts", WorkingPreset.Parts);
					command.Parameters.AddWithValue("Delay", WorkingPreset.Delay);
					command.Connection.Open();
					command.ExecuteNonQuery();
				}
			}
			else
			{
				presetWithSameName = new Preset(WorkingPreset);

				using (SqlConnection connection = new SqlConnection(DATA_SOURCE))
				{
					SqlCommand command = new SqlCommand("UPDATE Presets SET Name = @Name, Parts = @Parts, Delay = @Delay WHERE Name = @Name", connection);
					command.Parameters.AddWithValue("Name", WorkingPreset.Name);
					command.Parameters.AddWithValue("Parts", WorkingPreset.Parts);
					command.Parameters.AddWithValue("Delay", WorkingPreset.Delay);
					command.Connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}
	}
}
