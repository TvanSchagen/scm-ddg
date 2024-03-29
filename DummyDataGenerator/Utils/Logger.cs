﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DummyDataGenerator.Utils
{
	class Logger
	{

		public enum Level
		{
			DEBUG,
			INFO,
			WARN,
			ERROR
		}

		public static Level currentLevel = Level.DEBUG;

		public static void SetLevel(Level level)
		{
			currentLevel = level;
		}


		public static void Debug(string message)
		{
			WriteLine(message, Level.DEBUG);
		}

		public static void Info(string message)
		{
			WriteLine(message, Level.INFO);
		}

		public static void Info(bool newLine, string message)
		{
			if (newLine)
			{
				WriteLine(message, Level.INFO);
			} else
			{
				Write(message, Level.INFO);
			}
		}

		public static void Warn(string message)
		{
			WriteLine(message, Level.WARN);
		}

		public static void Error(string message)
		{
			WriteLine(message, Level.ERROR);
		}

		private static void Write(string message, Level level)
		{
			// don't log message if current logger level is below the message level
			if (level < currentLevel)
			{
				return;
			}
			// set colors
			Console.BackgroundColor = GetColors(level)[0];
			Console.ForegroundColor = GetColors(level)[1];
			Console.Write("[{0}]: {1}", DateTime.Now, message);
		}

		private static void WriteLine(string message, Level level)
		{
			// don't log message if current logger level is below the message level
			if (level < currentLevel)
			{
				return;
			}
			// set colors
			Console.BackgroundColor = GetColors(level)[0];
			Console.ForegroundColor = GetColors(level)[1];
			Console.WriteLine("[{0}]: {1}", DateTime.Now, message);
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
		}
		
		public static ConsoleColor[] GetColors(Level level)
		{
			ConsoleColor[] colors = new ConsoleColor[2];
			colors[0] = ConsoleColor.Black;
			colors[1] = ConsoleColor.White;
			switch (level)
			{
				case Level.DEBUG:
					colors[1] = ConsoleColor.Cyan;
					break;
				case Level.INFO:
					colors[1] = ConsoleColor.White;
					break;
				case Level.WARN:
					colors[0] = ConsoleColor.DarkYellow;
					colors[1] = ConsoleColor.Black;
					break;
				case Level.ERROR:
					colors[0] = ConsoleColor.DarkRed;
					break;

			}
			return colors;
		}

	}
}
