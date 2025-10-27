using GadgetApp.Models;
using System.Collections.Generic;

namespace GadgetApp.Services
{

    // Надає статичні методи для сортування колекцій.
    public static class SortingService
    {
        // Сортує список гаджетів за ціною, використовуючи алгоритм Quick Sort.
        public static void QuickSortByPrice(List<Gadget> gadgets)
        {
            if (gadgets == null || gadgets.Count <= 1)
                return;

            QuickSortRecursive(gadgets, 0, gadgets.Count - 1);
        }

        private static void QuickSortRecursive(List<Gadget> arr, int low, int high)
        {
            if (low < high)
            {
                int pi = Partition(arr, low, high);
                QuickSortRecursive(arr, low, pi - 1);
                QuickSortRecursive(arr, pi + 1, high);
            }
        }

        private static int Partition(List<Gadget> arr, int low, int high)
        {
            Gadget pivot = arr[high];
            int i = (low - 1);

            for (int j = low; j < high; j++)
            {
                if (arr[j].Price <= pivot.Price)
                {
                    i++;
                    (arr[i], arr[j]) = (arr[j], arr[i]);
                }
            }

            (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
            return i + 1;
        }
    }
}