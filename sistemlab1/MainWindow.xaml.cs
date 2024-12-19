using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace sistemlab1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Кнопка для загрузки процессов
        private async void btnLoadProcesses_Click(object sender, RoutedEventArgs e)
        {
            treeViewProcesses.Items.Clear();  // Очистка дерева перед загрузкой новых данных

            // Запускаем задачу для загрузки процессов в фоновом потоке
            await LoadProcessesAsync();
        }

        // Метод для асинхронной загрузки процессов
        private async Task LoadProcessesAsync()
        {
            // Запускаем получение процессов в фоновом потоке
            var processes = await Task.Run(() => Process.GetProcesses());

            // Строим дерево процессов
            var processTree = BuildProcessTree(processes);

            // Добавляем узлы в TreeView
            treeViewProcesses.Items.Clear();
            foreach (var rootNode in processTree)
            {
                treeViewProcesses.Items.Add(rootNode);
            }
        }

        // Строим дерево процессов, учитывая подчиненные отношения
        private List<TreeViewItem> BuildProcessTree(Process[] processes)
        {
            var nodes = new Dictionary<int, TreeViewItem>();

            // Создаем словарь узлов для каждого процесса
            foreach (var process in processes)
            {
                var node = new TreeViewItem
                {
                    Header = process.ProcessName,
                    Tag = process
                };
                nodes[process.Id] = node;
            }

            // Строим иерархию, добавляем дочерние процессы
            var rootNodes = new List<TreeViewItem>();
            foreach (var process in processes)
            {
                var parentProcess = GetParentProcess(process);

                if (parentProcess != null)
                {
                    // Добавляем в дочерний узел
                    nodes[parentProcess.Id].Items.Add(nodes[process.Id]);
                }
                else
                {
                    // Корневой процесс
                    rootNodes.Add(nodes[process.Id]);
                }
            }

            return rootNodes;
        }

        // Получаем родительский процесс через WMI
        private Process GetParentProcess(Process process)
        {
            try
            {
                var query = new ObjectQuery($"SELECT * FROM Win32_Process WHERE ProcessId = {process.Id}");
                var searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    int parentId = Convert.ToInt32(obj["ParentProcessId"]);
                    if (parentId != 0)
                    {
                        return Process.GetProcessById(parentId);
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        // Обработчик события выбора элемента в TreeView
        private void treeViewProcesses_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (treeViewProcesses.SelectedItem is TreeViewItem selectedItem)
            {
                var selectedProcess = selectedItem.Tag as Process;
                if (selectedProcess != null)
                {
                    txtProcessInfo.Text = $"ID: {selectedProcess.Id}\n" +
                                          $"Имя: {selectedProcess.ProcessName}\n" +
                                          $"Память: {selectedProcess.WorkingSet64 / 1024} KB\n" +
                                          $"Статус: {selectedProcess.Responding}\n";
                }
            }
        }
    }
}
