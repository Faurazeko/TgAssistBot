﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

using TgAssistBot.Models.OpenWeatherMap;
using TgAssistBot.Models.Database;

namespace TgAssistBot.Engines
{
	// For code 500 - light rain icon = "10d". See below a full list of codes
    //URL is http://openweathermap.org/img/wn/10d@2x.png
    static class ForecastImageEngine
    {
		//Fonts
		static FontCollection _fontCollection = new FontCollection();
		static FontFamily _fontFamily = _fontCollection.Add("ROBOTO.ttf");

		static Font _font50 = _fontFamily.CreateFont(50);
		static Font _font30 = _fontFamily.CreateFont(30);
		static Font _font16 = _fontFamily.CreateFont(16);
		static Font _font14 = _fontFamily.CreateFont(14);

		//Image settings
		static int _width = 1720;
		static int _halfWidth = _width / 2;
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

		static public Image GenerateImage(DbCity city)
		{

			var lastWeather = city.LastWeather;
			var sectionGap = 5;
			var sectionWidth = ((_xEnd - _xStart) / lastWeather.WeatherList.Count()) - sectionGap;

			var weatherList = lastWeather.WeatherList;

			var image = new Image<Rgba32>(_width, _height);

			image.Mutate(img =>
			{
				img.Fill(_backgroundColor);

				DrawCenterText(img, $"Прогноз погоды");
				DrawCityName(img, $"[{lastWeather.City.Name}]");
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
			var img = GenerateImage(city);
			img.SaveAsPng(path);
		}

		static public void SaveImageToStream(DbCity city, out MemoryStream stream)
        {
			stream = new MemoryStream();

			var img = GenerateImage(city);
			img.Save(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());

			stream.Position = 0;
		}

		static void DrawCenterText(IImageProcessingContext img, string text)
        {
			img.DrawText(
				new TextOptions(_font50)
				{
					HorizontalAlignment = HorizontalAlignment.Center,
					WrappingLength = _width,
					Origin = new PointF(_halfWidth, 0),
				}, text, _primaryColor);
		}

		static void DrawCityName(IImageProcessingContext img, string text)
		{
			img.DrawText(
				new TextOptions(_font50)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF(0, 0),
				}, text, _primaryColor);
		}

		static void DrawLegend(IImageProcessingContext img)
        {
			img.FillPolygon(_precipitationColor,
				new PointF(_halfWidth + 300, 5),
				new PointF(_halfWidth + 350, 5),
				new PointF(_halfWidth + 350, 55),
				new PointF(_halfWidth + 300, 55)
				);

			img.DrawText(
				new TextOptions(_font30)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF(_halfWidth + 360, 14),
				}, "- Осадки", _primaryColor);

			img.FillPolygon(_tempColor,
				new PointF(_halfWidth + 500, 5),
				new PointF(_halfWidth + 550, 5),
				new PointF(_halfWidth + 550, 55),
				new PointF(_halfWidth + 500, 55)
				);

			img.DrawText(
				new TextOptions(_font30)
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					WrappingLength = _width,
					Origin = new PointF(_halfWidth + 560, 14),
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
			TextOptions smallOptions = new(_font16)
			{
				Font = _font16,
				HorizontalAlignment = HorizontalAlignment.Center,
				WrappingLength = _width
			};

			var percents = 100;

			for (float i = _yStart; i <= _yEnd; i += (_percentInPixelsHeight * 10))
			{
				smallOptions.Origin = new PointF(_widthOffset / 2, i - (_font16.Size / 2));

				img.DrawText(smallOptions, $"{percents} %", _primaryColor);

				if (i != _yStart && percents != 0)
					img.DrawPolygon(_primaryColor, _brushThickness, new PointF(_xStart, i), new PointF(_xEnd, i));

				percents -= 10;
			}
		}

		static void DrawRainChanceInfo(IImageProcessingContext img, int sectionWidth, int sectionGap, List<WeatherList> weatherList, DbCity city)
        {
			void _drawNewDayInfo(DateTime forecastDateTime, int xPos)
            {
				img.DrawPolygon(new Color(new Rgba32(255, 255, 255, 100)), sectionGap,
					new PointF(xPos - 3, _yEnd + 2),
					new PointF(xPos - 3, _yStart)
					);

				img.DrawText(forecastDateTime.ToString("dd.MM"), _font14, _primaryColor, new PointF(xPos, _yEnd * 1.01f));
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

				img.DrawText(forecastDateTime.ToString("HH:mm"), _font14, _primaryColor, new PointF(i, _yEnd * 1.03f));

				index++;
			}
		}

		static void DrawTemperatureInfo(IImageProcessingContext img, List<WeatherList> weatherList, int sectionWidth, int sectionGap)
        {
			var trueSectionWidth = sectionWidth + sectionGap;

			var temps = new List<double>();

            foreach (var item in weatherList)
				temps.Add(item.Main.Temp);

			var minTemp = temps.Min();
			var maxTemp = temps.Max();
			var avgTemp = (minTemp + maxTemp) / 2;
			var halfHeight = _yEnd / 2;


            var index = 0;
			var points = new List<PointF>();

			for (int i = _xStart; i < _xEnd; i += trueSectionWidth)
			{
				var temp = temps[index];

				var stabilizedTemp = temp - avgTemp;

				float tempHeightPos = (float)(halfHeight - (_percentInPixelsHeight * stabilizedTemp));


                points.Add(new PointF(i, tempHeightPos));
				points.Add(new PointF(i + sectionWidth, tempHeightPos));

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