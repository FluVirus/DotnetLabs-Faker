﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Drawing;
using University.DotnetLabs.Lab2.FakerLibrary;

namespace University.DotneLabs.Lab2.ConsoleSection;

internal class Program
{
    private static string _pluginsPath = "Plugins";
    static void Main(string[] args)
    {
        ICollection<Generator> generators = new LinkedList<Generator>();
        string[] files = Directory.GetFiles(_pluginsPath);
        foreach (string filename in files)
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(filename);
                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    Type currentType = type;
                    while (currentType != null && currentType != typeof(object)) {
                        currentType = currentType.BaseType;
                        if (currentType == typeof(Generator)) {
                            object createdObject = Activator.CreateInstance(type);
                            if (createdObject != null)
                            {
                                generators.Add((Generator)createdObject);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }
        }
        Faker faker = new Faker();
        foreach (Generator generator in generators)
        {
            faker.Generators.TryAdd(generator.GeneratingType, generator);
        }
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(faker.Create<string>());
        }
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(faker.Create<Point>());
        }
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(faker.Create<decimal>());
        }
        List<double> list = faker.Create<List<double>>();
        foreach (double element in list)
        {
            Console.Write($"elemnet is {element}, ");
        }
        List<List<double>> dlist = faker.Create<List<List<double>>>();
        foreach (List<double> slist in dlist)
            foreach (double element in slist)
                Console.WriteLine($"Dlist element = {element}");

        FakerConfig configs = new FakerConfig();
        configs.Add<Point, int>(p => p.X, new IntValueGenerator(-10));
        Faker newFaker = new Faker(configs);
        Point p = newFaker.Create<Point>();
        Console.WriteLine(p);
    }
}
