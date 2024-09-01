using Spectre.Console;

namespace MySleepTimeSheduler;

internal class Program
{
	private static void Main(string[] args)
	{
		UserInputValues userInputValues = StorageHandler.LoadValues();

		UpdatePreviousWakeupSection(userInputValues.PreviousWakeup);
		UpdateFutureWakeupSection(userInputValues.FutureWakeup);

		DateTime previousWakeUpTime = CalculateDateTime(userInputValues.PreviousWakeup);
		DateTime futureWakeupTime = CalculateDateTime(userInputValues.FutureWakeup);

		TimeSpan dayTime = futureWakeupTime - previousWakeUpTime;
		TimeSpan sleepTime = dayTime * 1 / 3;
		DateTime goToBedTime = futureWakeupTime - sleepTime;
		TimeSpan awakeTime = goToBedTime - previousWakeUpTime;

		DisplayResults(userInputValues, previousWakeUpTime, futureWakeupTime, goToBedTime, awakeTime, sleepTime);

		StartProgress(goToBedTime, previousWakeUpTime);
	}

	private static void UpdateFutureWakeupSection(UserInputSection futureWakeup)
	{
		futureWakeup.Day = PromptDay("An welchem Tag willst du aufstehen?", new[] { "Heute", "Morgen" }, futureWakeup.Day);
		futureWakeup.Hour = PromptTime("Zu welcher Stunde willst du aufstehen?", 24, 1, futureWakeup.Hour);
		futureWakeup.Minute = PromptTime("Zu welcher Minute willst du aufstehen?", 12, 5, futureWakeup.Minute);
	}

	private static void UpdatePreviousWakeupSection(UserInputSection previousWakeup)
	{
		previousWakeup.Day = PromptDay("An welchem Tag bist du aufgewacht?", new[] { "Heute", "Gestern", "Vorgestern" }, previousWakeup.Day);
		previousWakeup.Hour = PromptTime("Zu welcher Stunde bist du aufgewacht?", 24, 1, previousWakeup.Hour);
		previousWakeup.Minute = PromptTime("Zu welcher Minute bist du aufgewacht?", 12, 5, previousWakeup.Minute);
	}

	private static string PromptDay(string title, IEnumerable<string> choices, string defaultValue)
	{
		choices = choices.Except([defaultValue])
			.Prepend(defaultValue);
		return AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title(title)
				.AddChoices(choices)
			//.DefaultValue(defaultValue) // will get added to Spectre in a future Version
		);
	}

	private static int PromptTime(string title, int max, int interval, int defaultValue)
	{
		var choices = Enumerable.Range(0, max)
			.Select(i => i * interval)
			.Except([defaultValue])
			.Prepend(defaultValue);
		return AnsiConsole.Prompt(
			new SelectionPrompt<int>()
				.Title(title)
				.AddChoices(choices)
			//.DefaultValue(defaultValue) // will get added to Spectre in a future Version
		);
	}

	private static int GetDeltaDay(string day)
	{
		return day switch
		{
			"Heute" => 0,
			"Gestern" => -1,
			"Vorgestern" => -2,
			"Morgen" => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(day), day, "Invalid day selection")
		};
	}

	private static DateTime CalculateDateTime(UserInputSection wakeup)
	{
		return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, wakeup.Hour, wakeup.Minute, 0).AddDays(GetDeltaDay(wakeup.Day));
	}

	private static void DisplayResults(UserInputValues userInputValues, DateTime previousBedTime, DateTime futureWakeupTime, DateTime goToBedTime, TimeSpan awakeTime, TimeSpan sleepTime)
	{
		AnsiConsole.MarkupLine(
			$"Du bist [green]{userInputValues.PreviousWakeup.Day}[/] um [green]{previousBedTime:HH:mm} Uhr[/] aufgewacht und willst [green]{userInputValues.FutureWakeup.Day}[/] um [green]{futureWakeupTime:HH:mm} Uhr[/] aufstehen.");
		AnsiConsole.MarkupLine($"Hierfür solltest du um [yellow]{goToBedTime:HH:mm} Uhr[/] schlafen gehen.");
		AnsiConsole.MarkupLine($"Anschließend solltest du dann [yellow]{sleepTime.Hours} Stunden und {sleepTime.Minutes} Minuten[/] schlafen.");
		AnsiConsole.MarkupLine($"Dann wirst du planmäßig [green]{futureWakeupTime:HH:mm} Uhr[/] aufwachen.");
		AnsiConsole.MarkupLine("");
		AnsiConsole.MarkupLine(
			$"Der Tag begann um [green]{previousBedTime:HH:mm} Uhr[/], dauert [yellow]{awakeTime.Hours} Stunden und {awakeTime.Minutes} Minuten[/] und endet um [yellow]{goToBedTime:HH:mm} Uhr[/].");
		AnsiConsole.MarkupLine($"Damit ist der Tag nur [yellow]{Math.Round((awakeTime.TotalMinutes / TimeSpan.FromHours(16).TotalMinutes) * 100, 0)}%[/] eines Standardtages lang.");
		AnsiConsole.MarkupLine($"Es sind bis jetzt schon [yellow]{(DateTime.Now - previousBedTime).Hours} Stunden und {(DateTime.Now - previousBedTime).Minutes} Minuten[/] vergangen.");
	}
	
	private static void StartProgress(DateTime goToBedTime, DateTime previousWakeUpTime)
	{
		DateTime now = DateTime.Now;

		AnsiConsole.Progress()
			.AutoClear(false)
			.HideCompleted(false)
			.Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn(), new SpinnerColumn())
			.Start(ctx =>
			{
				ProgressTask task1 = ctx.AddTask("[green]Go to bed | from application start[/]");
				ProgressTask task2 = ctx.AddTask("[green]Go to bed | from wakeup time      [/]");
				task1.MaxValue = (goToBedTime - now).TotalSeconds;
				task2.MaxValue = (goToBedTime - previousWakeUpTime).TotalSeconds;

				while (!ctx.IsFinished)
				{
					Task.Delay(750)
						.Wait();

					TimeSpan remainingTime = DateTime.Now - now;
					task1.Value = remainingTime.TotalSeconds;
					task2.Value = (DateTime.Now - previousWakeUpTime).TotalSeconds;
				}
			});
	}
}