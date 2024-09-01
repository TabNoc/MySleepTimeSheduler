using System.Text.Json;

namespace MySleepTimeSheduler;

internal static class StorageHandler
{
	private const string FilePath = "values.json";

	public static void SaveValues(UserInputValues values)
	{
		string json = JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
		File.WriteAllText(FilePath, json);
	}

	public static UserInputValues LoadValues()
	{
		if (!File.Exists(FilePath))
		{
			return new UserInputValues();
		}

		string json = File.ReadAllText(FilePath);
		UserInputValues userInputValues = JsonSerializer.Deserialize<UserInputValues>(json) ?? new UserInputValues();

		userInputValues.PreviousWakeup.Changed += (sender, args) => StorageHandler.SaveValues(userInputValues);
		userInputValues.FutureWakeup.Changed += (sender, args) => StorageHandler.SaveValues(userInputValues);
		
		return userInputValues;
	}
}

internal class UserInputValues
{
	public UserInputSection PreviousWakeup { get; init; } = new("Heute", 0, 0);
	public UserInputSection FutureWakeup { get; init; } = new("Heute", 0, 0);
}

internal class UserInputSection(string day, int hour, int minute)
{
	private string _day = day;
	private int _hour = hour;
	private int _minute = minute;
	public event EventHandler? Changed;

	public string Day
	{
		get => _day;
		set
		{
			_day = value;
			OnChanged();
		}
	}

	public int Hour
	{
		get => _hour;
		set
		{
			_hour = value;
			OnChanged();
		}
	}

	public int Minute
	{
		get => _minute;
		set
		{
			_minute = value;
			OnChanged();
		}
	}

	protected void OnChanged()
	{
		Changed?.Invoke(this, EventArgs.Empty);
	}
}