using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rgr
{
    class Program
    {
        static int processorCount = Environment.ProcessorCount; // Получаем колличество ядер
        static int section;
        static char[][] array; // Объявляем массив массивов
        static object locker = new object(); // Создаем блокирующий эллемент для крит. секции
        static int[] resultSeq; // Объявляем массив содержащий результат работы полседовательного метода
        static int[] resultThread; // Объявляем массив содержащий результат работы Thread метода
        static int[] resultTask; // Объявляем массив содержащий результат работы Task метода
        static int[] resultPar; // Объявляем массив содержащий результат работы Parallel метода

        static void Main(string[] args)
        {
            // Ввод размера исходного массива
            bool correct = true;
            int size = 0;
            while (correct) // Код внутри цикла повторяется пока введенное значение не пройдет верификацию
            {
                try // Верификация пользовательского ввода
                {
                    Console.Write("Введите размер исходного массива : ");
                    size = Convert.ToUInt16(Console.ReadLine());
                    Console.WriteLine();
                    if (size < 1 || size > 20000) // Условие проверки ввода, если значение вне заданных диапазонов выбрасывается ошибка
                    {
                        throw new Exception(); // Бросаем Exception
                    }
                    correct = false; // Прерываем цикл
                }
                catch (Exception) // Вызывается при возникновении Exception
                {
                    Console.WriteLine("Размер исходного массива 1 - 20000");
                }
            }
            // Блок инициализации (создание исходных данных и счетчика времени)
            Stopwatch sw = new Stopwatch(); // Создаем счетчик времени
            Random random = new Random(Convert.ToInt16(DateTime.Now.Second)); // Создаем объект класса Random, передаем текущее время для разных исходдных данных при каждом запуске
            array = new char[size][]; // Задаем кол-во строк исх. масс.
            // Выделяем память массивам результатов
            resultSeq = new int[size];
            resultThread = new int[size];
            resultTask = new int[size];
            resultPar = new int[size];
            section = array.Length / processorCount; // Задаем размер отрезка данных для каждого потока



            // Заполнение массива
            Console.WriteLine("Генерация массива символов.");
            for (int i = 0; i < array.Length; i++) // Проход по строкам массива (GetLength(0) - возвращает размер измерения массива)
            {
                array[i] = new char[size]; // Задаем кол-во столбцов исх. масс.
                for (int j = 0; j < array[i].Length; j++) // Проход по столбцам массива
                {
                    array[i][j] = (char)random.Next(33, 122); // Заполнение массива с помощью генерации рандомных чисел в промежутке 33 - 122
                }
            }
            Console.WriteLine();
            Console.WriteLine("Массив сгенерирован!");
            string outArr; // Хранит ответ на вопрос о выводе результата
            try
            {
                Console.WriteLine("Нажмите 'y' для вывода исходного массива!");
                outArr = Convert.ToString(Console.ReadLine()).ToLower(); // Считывание строки из консоли
                if (outArr.Equals("y")) // Проверка, содержит ли введенная строка символ "Y"
                {
                    for (int i = 0; i < array.Length; i++) // Проход по строкам массива (GetLength(0) - возвращает размер измерения массива)
                    {
                        Console.WriteLine(); // Вывод массива строками
                        for (int j = 0; j < array[i].Length; j++) // Проход по столбцам массива
                        {
                            Console.Write(array[i][j]); // Вывод массива
                        }
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); }
            Console.WriteLine();



            // -- Последовательный метод
            Console.WriteLine("Начало выполнения последовательного метода.");
            sw.Start(); // Запуск счетчика времени                   
            sequential(); // Вызываем метод поиска
            sw.Stop(); // Остановка счетчика времени
            Console.WriteLine("--> Время выполнения : " + sw.ElapsedMilliseconds.ToString() + " Миллисекунд"); // Получаем и выводим время работы счетчика
            confirm(resultSeq); // Вызываем метод, который спрашивает о выводе результата
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine();
            sw.Reset(); // Обнуление счетчика времени




            // -- Параллельный Thread метод
            Thread[] ThreadArray = new Thread[processorCount]; // Создаем массив потоков
            Console.WriteLine("Начало выполнения параллельного Thread метода.");
            sw.Start(); // Запуск счетчика времени
            for (int i = 0; i < ThreadArray.Length; i++) // Этот цикл проходится по массиву потоков
            {
                ThreadArray[i] = new Thread(thread); // Создание нового потока, выполняющего параллельный метод "thread"
                ThreadArray[i].Start(i); // Запуск потока
            }
            for (int i = 0; i < ThreadArray.Length; i++) // Этот цикл проходится по массиву потоков
            {
                ThreadArray[i].Join(); // Ожидание выполнения всех потоков
            }
            sw.Stop(); // Остановка счетчика времени
            Console.WriteLine("--> Время выполнения : " + sw.ElapsedMilliseconds.ToString() + " Миллисекунд"); // Получаем и выводим время работы счетчика
            confirm(resultThread);
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine();
            sw.Reset(); // Обнуление счетчика времени




            // -- Параллельный Task метод
            Task[] TaskArray = new Task[processorCount]; // Создаем массив задач
            Console.WriteLine("Начало выполнения параллельного Task метода.");
            sw.Start(); // Запуск счетчика времени
            for (int i = 0; i < TaskArray.Length; i++)
            {
                TaskArray[i] = new Task(task, i);
                TaskArray[i].Start();
            }
            Task.WaitAll(TaskArray);
            sw.Stop(); // Остановка счетчика времени
            Console.WriteLine("--> Время выполнения : " + sw.ElapsedMilliseconds.ToString() + " Миллисекунд"); // Получаем и выводим время работы счетчика
            confirm(resultTask);
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine();
            sw.Reset(); // Обнуление счетчика времени




            // Проход по массиву с использованием класса Parallel
            Console.WriteLine("Начало выполнения параллельного Parallel метода.");
            sw.Start(); // Запуск счетчика времени
            System.Threading.Tasks.Parallel.For(0, array.Length, new ParallelOptions() { MaxDegreeOfParallelism = processorCount }, i =>
            { // Проход по строкам массива 
                int count = 0;
                for (int j = 0; j < array[i].Length; j++)
                {
                    if (array[i][j].Equals('0') || array[i][j].Equals('1') || array[i][j].Equals('2') || array[i][j].Equals('3') || array[i][j].Equals('4') || array[i][j].Equals('5') || array[i][j].Equals('6') || array[i][j].Equals('7') || array[i][j].Equals('8') || array[i][j].Equals('9'))
                    {
                        Interlocked.Add(ref count, Convert.ToInt32(array[i][j].ToString()));  // Потокобезопасное инкрементирование
                    }
                }
                resultPar[i] = count; // Записываем результат поиска в строке
            });
            sw.Stop(); // Остановка счетчика времени
            Console.WriteLine("--> Время выполнения : " + sw.ElapsedMilliseconds.ToString() + " Миллисекунд"); // Получаем и выводим время работы счетчика
            confirm(resultPar);
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine();
            sw.Reset(); // Обнуление счетчика времени




            // Выборка из массива с использованием PLINQ запроса
            Console.WriteLine("Начало выполнения PLINQ запроса.");
            sw.Start(); // Запуск счетчика времени
            int[] result = array.AsParallel().Select(
                array =>
                {
                    int count = 0;
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (char.IsDigit(array[j])){
                            count += Convert.ToInt32(array[j].ToString());
                        }
                    }
                    return count;
                }
                ).ToArray();
            sw.Stop(); // Остановка счетчика времени
            Console.WriteLine("--> Время выполнения : " + sw.ElapsedMilliseconds.ToString() + " Миллисекунд"); // Получаем и выводим время работы счетчика
            confirm(result);
            Console.WriteLine("Завершено!");
            Console.ReadKey();
        }
        static void confirm(int[] array) // Этот метод спрашивает выводить ли результат
        {
            string confirm; // Хранит ответ на вопрос о выводе результата
            try
            {
                Console.WriteLine("Нажмите 'y', если хотите увидеть результат!");
                confirm = Convert.ToString(Console.ReadLine()).ToLower(); // Считывание строки из консоли
                if (confirm.Equals("y")) // Проверка, содержит ли введенная строка символ "Y"
                {
                    Console.WriteLine("Результат работы : ");
                    Print(array); // Вызов метода вывода массива, который содержит результат работы последовательного метода
                }
            }
            catch (Exception e) { Console.WriteLine(e.StackTrace); }
        }
        static void Print(int[] array) // Этот метод выводит результат
        {
            for (int i = 0; i < array.Length; i++)
            {
                Console.WriteLine("Сумма в строке " + (i + 1) + " : " + array[i]);
            }
        }
        static void sequential() // Последовательный метод подсчета вхождения цифр в каждую строку исходного массива
        {
            int count=0;
            for (int i = 0; i < array.Length; i++)
            {
                count = 0;
                for (int j = 0; j < array[i].Length; j++)
                {
                    if (array[i][j].Equals('0') || array[i][j].Equals('1') || array[i][j].Equals('2') || array[i][j].Equals('3') || array[i][j].Equals('4') || array[i][j].Equals('5') || array[i][j].Equals('6') || array[i][j].Equals('7') || array[i][j].Equals('8') || array[i][j].Equals('9'))
                    {
                        count+=Convert.ToInt32( array[i][j].ToString());
                    }
                }
                resultSeq[i] = count;
            }
        }
        static void thread(object o) // Thread - параллельныый метод подсчета вхождения цифр в каждую строку исходного массива
        {
            int thread = (int)o;
            int count;
            for (int i = thread * section; i < ((thread == processorCount - 1) ? array.Length : (thread + 1) * section); i++) // Проход по строкам массива. Тернарное условие определяет является ли выполняющий поток последним и если да, отдает ему оставшийся отрезок данных
            {
                count = 0;
                for (int j = 0; j < array[i].Length; j++) // Проход по столбцам массива
                {
                    if (array[i][j].Equals('0') || array[i][j].Equals('1') || array[i][j].Equals('2') || array[i][j].Equals('3') || array[i][j].Equals('4') || array[i][j].Equals('5') || array[i][j].Equals('6') || array[i][j].Equals('7') || array[i][j].Equals('8') || array[i][j].Equals('9'))
                    {
                        count += Convert.ToInt32(array[i][j].ToString());
                    }
                }
                resultThread[i] = count;
            }
        }
        static void task(object o) // Task - параллельныый метод подсчета вхождения цифр в каждую строку исходного массива
        {
            int task = (int)o;
            int count;
            for (int i = task * section; i < ((task == processorCount - 1) ? array.Length : (task + 1) * section); i++) // Проход по строкам массива. Тернарное условие определяет является ли выполняющий поток последним и если да, отдает ему оставшийся отрезок данных
            {
                count = 0;
                for (int j = 0; j < array[i].Length; j++) // Проход по столбцам массива
                {
                    if (array[i][j].Equals('0') || array[i][j].Equals('1') || array[i][j].Equals('2') || array[i][j].Equals('3') || array[i][j].Equals('4') || array[i][j].Equals('5') || array[i][j].Equals('6') || array[i][j].Equals('7') || array[i][j].Equals('8') || array[i][j].Equals('9'))
                    {
                        count += Convert.ToInt32(array[i][j].ToString());
                    }
                }
                resultTask[i] = count;
            }
        }
    }
}