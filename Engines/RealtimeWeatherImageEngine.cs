using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

using TgAssistBot.Models.OpenWeatherMap;
using TgAssistBot.Models.Database;
using TgAssistBot.Models.WeatherApi;

namespace TgAssistBot.Engines
{
	static class RealtimeWeatherImageEngine
	{
		//Fonts
		static FontCollection _fontCollection = new FontCollection();
		static FontFamily _fontFamily = _fontCollection.Add("ROBOTO.ttf");
		static int _fontSize = 25;
		static Font _font25 = _fontFamily.CreateFont(_fontSize);

		//Image settings
		static int _width = 650;
		static int _height = 475;

		//Brush settings
		static int _brushThickness = 3;

		//Forecast zone 
		static int _sidesOffset = 25 - _brushThickness;

		//Colors
		static Color _precipitationColor = new Color(new Rgba32(71, 69, 240));
		static Color _backgroundColor = new Color(new Rgba32(54, 57, 63));
		static Color _primaryColor = new Color(new Rgba32(185, 187, 190));

		static public Image GenerateImage(WeatherApiResponse response)
		{
			var weather = response.Current;
			var image = new Image<Rgba32>(_width, _height);

			image.Mutate(img =>
			{
				img.Fill(_backgroundColor);

				DrawTime(img, response);
				DrawCityName(img, response);
				DrawTemperatureInfo(img, weather);
				DrawUvIndexInfo(img, weather);
				DrawOtherInfo(img, weather);
			});

			return image;
		}

		static public void SaveImageAsPng(WeatherApiResponse response, string path)
		{
			var img = GenerateImage(response);
			img.SaveAsPng(path);
		}

		static public void SaveImageToStream(WeatherApiResponse response, out MemoryStream stream)
		{
			stream = new MemoryStream();

			var img = GenerateImage(response);
			img.Save(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

			stream.Position = 0;
		}

		static private void DrawTime(IImageProcessingContext img, WeatherApiResponse response)
		{
			var dtFormat = "dd.MM HH:mm";

			var text =
			$"Текущее время: {DateTime.Parse(response.Location.LocalTime).ToString(dtFormat)}\n" +
			$"Последнее обновление: {DateTime.Parse(response.Current.LastUpdated).ToString(dtFormat)}";

			img.DrawText(
				new TextOptions(_font25)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Origin = new PointF(_sidesOffset, _sidesOffset),
				}, text, _primaryColor);
		}

		static private void DrawCityName(IImageProcessingContext img, WeatherApiResponse response)
        {
			var textOptions = new TextOptions(_font25) { HorizontalAlignment = HorizontalAlignment.Left };
            var text = $"[{response.Location.Name}]";

			var textWidth = TextMeasurer.Measure(text, textOptions).Width;
			textOptions.Origin = new System.Numerics.Vector2(_width - textWidth - _sidesOffset, _sidesOffset / 2);

			img.DrawText(textOptions, text, _primaryColor);
		}

		static private void DrawTemperatureInfo(IImageProcessingContext img, CurrentWeather weather)
		{
			var rectWidth = 300;
			var topOffset = (3 * _fontSize) + _sidesOffset;
			var tempTableHeight = 5 * _fontSize;

			// draw frame
			img.DrawPolygon(_precipitationColor, _brushThickness, 
				new PointF(_sidesOffset - _brushThickness, topOffset),
				new PointF(rectWidth, topOffset),
				new PointF(rectWidth, tempTableHeight + topOffset),
				new PointF(_sidesOffset - _brushThickness, tempTableHeight + topOffset)
				);

			img.DrawText(
				new TextOptions(_font25)
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					Origin = new PointF(rectWidth / 2, topOffset),
				}, "Температура", _primaryColor);

			img.DrawText(
				new TextOptions(_fontFamily.CreateFont(15))
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					Origin = new PointF(rectWidth / 2, topOffset + _fontSize * 1.25f),
				}, "(В Цельсиях)", _primaryColor);

			img.DrawText(
				new TextOptions(_font25)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Origin = new PointF(_sidesOffset, topOffset + _fontSize * 2),
				}, $"Реальная: {weather.TempC}° \nПо ощущениям: {weather.FeelslikeC}°", _primaryColor);
		}

		static private void DrawUvIndexInfo(IImageProcessingContext img, CurrentWeather weather)
		{
			var descText = $"";
			var colorText = "";
			Color uvColor = Color.FromRgb(62, 167, 45);
			var textColor = Color.White;

			if (weather.Uv <= 2)
			{
				descText += "Меры защиты не нужны. Для большинства людей нет опасности вне помещений.";
				colorText = "Зелёный\nPMS 375";
			}
			else if (weather.Uv <= 5)
			{
				descText += "Необходима защита. В полуденные часы желательно находиться в тени или в помещении, а вне помещения нужно использовать " +
					"солнцезащитную одежду, шляпу и очки, открытую кожу рекомендуется защищать кремом";
				uvColor = Color.FromRgb(255, 243, 0);
				colorText = "Жёлтый\nPMS 102";
				textColor = Color.Black;
			}
			else if (weather.Uv <= 7)
			{
				descText += "Необходима защита. Обязательно используйте солнцезащитные средства, сократите " +
					"время нахождения под солнечными лучами в период с 10 до 16 часов.";
				uvColor = Color.FromRgb(241, 139, 0);
				colorText = "Оранжевый\nPMS 151";
				textColor = Color.Black;
			}
			else if (weather.Uv <= 10)
			{
				descText += "Необходима усиленная защита. Обязательно используйте солнцезащитные средства, " +
					"минимизируйте время нахождения под солнечными лучами в период с 10 до 16 часов.";
				uvColor = Color.FromRgb(229, 50, 16);
				colorText = "Красный\nPMS 032";
			}
			else
			{
				descText += "Нужна максимальная защита. Обязательно используйте сильные солнцезащитные средства, " +
					"избегайте нахождения под солнечными лучами. Глаза и открытая кожа могут получить повреждения за считанные минуты.";
				uvColor = Color.FromRgb(181, 103, 164);
				colorText = "Фиолетовый\nPMS 265";
			}

			var textSize = TextMeasurer.Measure(colorText, new TextOptions(_font25));

			var xOffset = 15;
			var xLeftPoint = 370 - xOffset + _sidesOffset;
			var xRightPoint = xLeftPoint + textSize.Width + (xOffset * 2);

			//draw uv color rect
			img.FillPolygon(uvColor,
				new PointF(xLeftPoint, _sidesOffset + _fontSize * 4.25f),
				new PointF(xRightPoint, _sidesOffset + _fontSize * 4.25f),
				new PointF(xRightPoint, _sidesOffset + _fontSize * 6.75f),
				new PointF(xLeftPoint, _sidesOffset + _fontSize * 6.75f)
				);

			var uvTextOptions = new TextOptions(_fontFamily.CreateFont(20))
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				WrappingLength = 280,
				Origin = new PointF(_sidesOffset + 310, _sidesOffset + _fontSize * 7),
			};

			var uvTextSize = TextMeasurer.Measure(descText, uvTextOptions);

			img.DrawText(uvTextOptions, descText, _primaryColor);

			//draw frame
			img.DrawPolygon(_precipitationColor, _brushThickness,
				new PointF(_sidesOffset + 300, _sidesOffset + _fontSize * 3),
				new PointF(_sidesOffset + 600, _sidesOffset + _fontSize * 3),
				new PointF(_sidesOffset + 600, _sidesOffset + _fontSize * 7.25f + uvTextSize.Height),
				new PointF(_sidesOffset + 300, _sidesOffset + _fontSize * 7.25f + uvTextSize.Height)
				);

			img.DrawText(
				new TextOptions(_font25)
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					Origin = new PointF(_sidesOffset + 450, _sidesOffset + _fontSize * 3),
				}, "УФ-Индекс", _primaryColor);

			img.DrawText(
				new TextOptions(_font25)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF(_sidesOffset + 305, _sidesOffset + _fontSize * 4.75f),
				}, $"{weather.Uv} -", _primaryColor);

			img.DrawText(
				new TextOptions(_font25)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF(_sidesOffset + 370, _sidesOffset + _fontSize * 4.25f),
				}, colorText, textColor);
		}

		static private void DrawOtherInfo(IImageProcessingContext img, CurrentWeather weather)
		{
			var font20 = _fontFamily.CreateFont(20);

			img.DrawText(new TextOptions(_font25)
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				WrappingLength = _width,
				Origin = new PointF(150, _sidesOffset + _fontSize * 9),
			}, "Разное", _primaryColor);

			string[] strings = new string[] 
			{
				$"Давление: {ConvertMbToMercuryMm(weather.PressureMb).ToString("G3")} мм рт.ст.",
				$"Осадки: {weather.PrecipMm} мм.",
				$"Влажность: {weather.Humidity}%",
				$"Облачность: {weather.Cloud}%",
				$"Ветер: {weather.WindKph}км/ч, ({weather.WindDegree}°, {weather.WindDir})",
				$"Описание: {weather.Condition.Text}"
			};

			var textOptions = new TextOptions(font20)
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				WrappingLength = 300,
				Origin = new PointF(_sidesOffset, _sidesOffset + _fontSize * 10),
			};

			float textHeight = 0;

            foreach (var item in strings)
            {
				img.DrawText(textOptions, item, _primaryColor);
				textOptions.Origin += new System.Numerics.Vector2(0, _fontSize);

				textHeight += TextMeasurer.Measure(item, textOptions).Height;
			}

			var yTopPos = _sidesOffset + (_fontSize * 8) + 20;
			var yBottomPos = yTopPos + textHeight * 1.3f;

			//draw frame
			img.DrawPolygon(_precipitationColor, _brushThickness,
				new PointF(_sidesOffset - _brushThickness, yTopPos),
				new PointF(300, yTopPos),
				new PointF(300, yBottomPos),
				new PointF(_sidesOffset - _brushThickness, yBottomPos)
				);
		}

		private static double ConvertMbToMercuryMm(double pressureMb) => pressureMb / 1.333;
	}
}