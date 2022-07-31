using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

using TgAssistBot.Models.OpenWeatherMap;
using TgAssistBot.Models.Database;

namespace TgAssistBot.Engines
{
    static class ForecastImageEngine
    {
		//Fonts
		static FontCollection _fontCollection = new FontCollection();
		static FontFamily _fontFamily = _fontCollection.Add("ROBOTO.ttf");

		//Image settings
		static int _width = 1720;
		static int _height = 720;

		//Brush settings
		static int _brushThickness = 3;

		//Forecast zone 
		static int _widthOffset = 60 - _brushThickness; // from both sides
		static int _topOffset = 70;

		static int _yStart = _topOffset + _brushThickness;
		static int _yEnd = 673;

		static int _xStart = _widthOffset + _brushThickness;
		static int _xEnd = _width - _xStart;

		static int _percentInPixelsHeight = (_yEnd - _yStart) / 100;

		//Colors
		static Color _precipitationColor = new Color(new Rgba32(71, 69, 240));
		static Color _tempColor = new Color(new Rgba32(243, 222, 44));
		static Color _backgroundColor = new Color(new Rgba32(54, 57, 63));
		static Color _primaryColor = new Color(new Rgba32(185, 187, 190));

		static public Image GenerateImage(WeatherMapResponse response, DbCity city)
		{
			var sectionGap = 5;
			var sectionWidth = ((_xEnd - _xStart) / response.WeatherList.Count()) - sectionGap;

			var weatherList = response.WeatherList;

			var image = new Image<Rgba32>(_width, _height);

			image.Mutate(img =>
			{
				img.Fill(_backgroundColor);

				DrawCenterText(img, $"Прогноз погоды");
				DrawCityName(img, $"[{response.City.Name}]");
				DrawLegend(img);

				DrawMainFrame(img);
				DrawHorizontalMeasures(img);
				DrawRainChanceInfo(img, sectionWidth, sectionGap, weatherList, city);
				DrawTemperatureInfo(img, weatherList, sectionWidth, sectionGap);

			});

			return image;
		}

		static public void SaveImageAsPng(DbCity city, string path)
        {
			var img = GenerateImage(city.LastWeather, city);
			img.SaveAsPng(path);
		}

		static public void SaveImageToStream(DbCity city, out MemoryStream stream)
        {
			stream = new MemoryStream();

			var img = GenerateImage(city.LastWeather, city);
			img.Save(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

			stream.Position = 0;
		}

		static void DrawCenterText(IImageProcessingContext img, string text)
        {
			img.DrawText(
				new TextOptions(_fontFamily.CreateFont(50))
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = _width,
					Origin = new PointF(_width / 2, 0),
				}, text, _primaryColor);
		}

		static void DrawCityName(IImageProcessingContext img, string text)
		{
			img.DrawText(
				new TextOptions(_fontFamily.CreateFont(50))
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF(0, 0),
				}, text, _primaryColor);
		}

		static void DrawLegend(IImageProcessingContext img)
        {
			img.FillPolygon(_precipitationColor,
				new PointF((_width / 2) + 300, 5),
				new PointF((_width / 2) + 350, 5),
				new PointF((_width / 2) + 350, 55),
				new PointF((_width / 2) + 300, 55)
				);

			img.DrawText(
				new TextOptions(_fontFamily.CreateFont(30))
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF((_width / 2) + 360, 14),
				}, "- Осадки", _primaryColor);

			img.FillPolygon(_tempColor,
				new PointF((_width / 2) + 500, 5),
				new PointF((_width / 2) + 550, 5),
				new PointF((_width / 2) + 550, 55),
				new PointF((_width / 2) + 500, 55)
				);

			img.DrawText(
				new TextOptions(_fontFamily.CreateFont(30))
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF((_width / 2) + 560, 14),
				}, "- Температура", _primaryColor);
		}

		static void DrawMainFrame(IImageProcessingContext img)
        {
			img.DrawPolygon(_primaryColor, _brushThickness,
				new PointF(_widthOffset, _topOffset),
				new PointF(_width - _widthOffset, _topOffset),
				new PointF(_width - _widthOffset, 676),
				new PointF(_widthOffset, 676)
			);
		}

		static void DrawHorizontalMeasures(IImageProcessingContext img)
        {
			Font smallFont = _fontFamily.CreateFont(16);

			TextOptions smallOptions = new(smallFont)
			{
				Font = smallFont,
				HorizontalAlignment = HorizontalAlignment.Center,
				WrappingLength = _width
			};

			var percents = 100;

			//smallOptions.Origin = new PointF(_widthOffset * 1.55f, _yStart - smallFont.Size * 1.5f);
			//img.DrawText(smallOptions, "Вероятность осадков", _primaryColor);

			for (float i = _yStart; i <= _yEnd; i += (_percentInPixelsHeight * 10))
			{
				smallOptions.Origin = new PointF(_widthOffset / 2, i - (smallFont.Size / 2));

				img.DrawText(smallOptions, $"{percents} %", _primaryColor);

				if (i != _yStart && percents != 0)
					img.DrawPolygon(_primaryColor, _brushThickness, new PointF(_xStart, i), new PointF(_xEnd, i));

				percents -= 10;
			}
		}

		static void DrawRainChanceInfo(IImageProcessingContext img, int sectionWidth, int sectionGap, List<WeatherList> weatherList, DbCity city)
        {
			Font font = _fontFamily.CreateFont(14);

			void _drawNewDayInfo(DateTime forecastDateTime, int xPos)
            {
				img.DrawPolygon(new Color(new Rgba32(255, 255, 255, 100)), sectionGap,
					new PointF(xPos - 3, _yEnd + 2),
					new PointF(xPos - 3, _yStart)
					);

				img.DrawText(forecastDateTime.ToString("dd.MM"), font, _primaryColor, new PointF(xPos, _yEnd * 1.01f));
			}

			void _drawRainPolygon(int xPos, float rainHeight)
            {
				img.FillPolygon(_precipitationColor,
					new PointF(xPos, _yEnd + 2),
					new PointF(xPos, _yEnd - rainHeight),
					new PointF(xPos + sectionWidth, _yEnd - rainHeight),
					new PointF(xPos + sectionWidth, _yEnd + 2)
					);
			}

			var index = 0;

			for (int i = _xStart; i < _xEnd; i += sectionWidth + sectionGap)
			{
				var weather = weatherList[index];

				var percentage = (float)weather.Pop * 100;
				float rainHeight = percentage * _percentInPixelsHeight;

				_drawRainPolygon(i, rainHeight);

				var forecastDateTime = DateTime.Parse(weather.DtTxtUTC).AddTicks(city.UtcOffset.Ticks);

				if (index % 8 == 0)
					_drawNewDayInfo(forecastDateTime, i);

				img.DrawText(forecastDateTime.ToString("HH:mm"), font, _primaryColor, new PointF(i, _yEnd * 1.03f));

				index++;
			}
		}

		static void DrawTemperatureInfo(IImageProcessingContext img, List<WeatherList> weatherList, int sectionWidth, int sectionGap)
        {
			var trueSectionWidth = sectionWidth + sectionGap;

			var temps = new List<double>();

            foreach (var item in weatherList)
				temps.Add(item.Main.Temp);

			var maxTemp = temps.Max();
			var tempYStart = _yStart + 100;

			var index = 0;
			var points = new List<PointF>();

			for (int i = _xStart; i < _xEnd; i += trueSectionWidth)
			{
				var temp = temps[index];

				points.Add(new PointF(i, _yEnd - (_percentInPixelsHeight * (float)temp)));
				points.Add(new PointF(i + sectionWidth, _yEnd - (_percentInPixelsHeight * (float)temp)));

				index++;
			}

			Font font = _fontFamily.CreateFont(14);

			var options = new TextOptions(font)
			{
				HorizontalAlignment = HorizontalAlignment.Center,
			};


			img.DrawLines(_tempColor, _brushThickness, points.ToArray());

			var y = 0;
			for (int i = 0; i < points.Count() / 2; i++)
            {
				options.Origin = new PointF(points[y].X + (sectionWidth / 2), points[y].Y - (font.Size * 1.2f) );

				var text = ((int)Math.Ceiling(temps[i])).ToString();

				img.DrawText(options, text, _tempColor);
				y += 2;
            }
		}
    }
}