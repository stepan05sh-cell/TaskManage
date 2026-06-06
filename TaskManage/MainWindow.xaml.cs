using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace TaskManager
{
    // --- Модель задачи ---
    public class TaskItem : INotifyPropertyChanged
    {
        private bool _isCompleted;

        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // --- Команда для привязки к кнопкам (RelayCommand) ---
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- ViewModel главного окна ---
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _newTaskText;

        public ObservableCollection<TaskItem> Tasks { get; set; }

        public string NewTaskText
        {
            get => _newTaskText;
            set
            {
                if (_newTaskText != value)
                {
                    _newTaskText = value;
                    OnPropertyChanged(nameof(NewTaskText));
                    (AddTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public int TotalCount => Tasks.Count;
        public int ActiveCount => Tasks.Count(t => !t.IsCompleted);
        public int CompletedCount => Tasks.Count(t => t.IsCompleted);

        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }

        public MainViewModel()
        {
            Tasks = new ObservableCollection<TaskItem>();

            // Подписываемся на изменение коллекции для обновления счётчиков
            Tasks.CollectionChanged += (s, e) => UpdateCounts();

            AddTaskCommand = new RelayCommand(
                execute: _ => AddTask(),
                canExecute: _ => !string.IsNullOrWhiteSpace(NewTaskText)
            );

            DeleteTaskCommand = new RelayCommand(
                execute: parameter =>
                {
                    if (parameter is TaskItem task)
                    {
                        Tasks.Remove(task);
                    }
                }
            );
        }

        private void AddTask()
        {
            var newTask = new TaskItem
            {
                Title = NewTaskText.Trim(),
                CreatedAt = DateTime.Now,
                IsCompleted = false
            };

            // Подписываемся на изменение состояния конкретной задачи для обновления счётчиков
            newTask.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TaskItem.IsCompleted))
                {
                    UpdateCounts();
                }
            };

            Tasks.Add(newTask);
            NewTaskText = string.Empty; // Очистка поля ввода
        }

        private void UpdateCounts()
        {
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(CompletedCount));
            (AddTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}