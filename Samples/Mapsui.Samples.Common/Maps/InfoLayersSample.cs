using System.Collections.Generic;
using System.Linq;
using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Utilities;

namespace Mapsui.Samples.Common.Maps
{
    public static class InfoLayersSample
    {
        private const string InfoLayerName = "Info Layer";
        private const string HoverLayerName = "Hover Layer (Info events)";
        private const string HoverEventLayerName = "Hover Layer (Layer events)";
        private const string PolygonLayerName = "Polygon Layer";
        private const string LineLayerName = "Line Layer";

        public static Map CreateMap()
        {
            var map = new Map();

            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            map.Layers.Add(CreateInfoLayer(map.Envelope));
            map.Layers.Add(CreateHoverLayer(map.Envelope));
            map.Layers.Add(CreatePolygonLayer());
            map.Layers.Add(CreateLineLayer());
            map.Layers.Add(CreateHoverEventLayer(map.Envelope));

            map.InfoLayers.Add(map.Layers.First(l => l.Name == InfoLayerName));
            map.InfoLayers.Add(map.Layers.First(l => l.Name == PolygonLayerName));
            map.InfoLayers.Add(map.Layers.First(l => l.Name == LineLayerName));
            map.HoverLayers.Add(map.Layers.First(l => l.Name == HoverLayerName));

            return map;
        }

        private static ILayer CreatePolygonLayer()
        {
            var features = new Features { CreatePolygonFeature(), CreateMultiPolygonFeature(), CreatePolygonFeatureWithEventHandlers() };
            var provider = new MemoryProvider(features);

            var layer = new MemoryLayer
            {
                Name = PolygonLayerName,
                DataSource = provider,
                Style = null,
                IncludeFeaturesInUserEvents = true
            };

            // Just an example to show how to simply disable zoom-in on double-click on this layer
            layer.DoubleTap += (sender, args) =>
            {
                if (args.Feature != null)
                    args.Handled = true;
            };

            // Just an example to show how to simply disable panning when the mouse/finger is on a feature on this layer
            layer.TouchStarted += (sender, args) =>
            {
                if (args.Feature != null)
                    args.Handled = true;
            };

            return layer;
        }

        private static ILayer CreateLineLayer()
        {
            return new MemoryLayer
            {
                Name = LineLayerName,
                DataSource = new MemoryProvider(CreateLineFeature()),
                Style = null
            };
        }

        private static Feature CreateMultiPolygonFeature()
        {
            var feature = new Feature
            {
                Geometry = CreateMultiPolygon(),
                ["Name"] = "Multipolygon 1"
            };
            feature.Styles.Add(new VectorStyle { Fill = new Brush(Color.Gray), Outline = new Pen(Color.Black) });
            return feature;
        }

        private static Feature CreatePolygonFeature()
        {
            var feature = new Feature
            {
                Geometry = CreatePolygon(),
                ["Name"] = "Polygon 1"
            };
            feature.Styles.Add(new VectorStyle());
            return feature;
        }

        private static Feature CreatePolygonFeatureWithEventHandlers()
        {
            var feature = new Feature
            {
                Geometry = CreateAlternativePolygon(),
                ["Name"] = "Polygon with feature event handlers"
            };
            feature.Styles.Add(new VectorStyle() { Fill = new Brush(Color.Blue), Outline = new Pen(Color.Red) });
            feature.HoveredOnce += (sender, args) => {
                feature.Styles.First().Opacity = (args.ModifierCtrl) ? 0.4f : 0.2f;
                args.MapNeedsRefresh = true;
                args.Handled = true;
            };
            feature.HoverStopped += (sender, args) => {
                feature.Styles.First().Opacity = 1.0f;
                args.MapNeedsRefresh = true;
                args.Handled = true;
            };
            feature.SingleTap += (sender, args) =>
            {
                ((VectorStyle) feature.Styles.First()).Fill.Color = Color.Green;
            };
            return feature;
        }

        private static Feature CreateLineFeature()
        {
            return new Feature
            {
                Geometry = CreateLine(),
                ["Name"] = "Line 1",
                Styles = new List<IStyle> { new VectorStyle{ Line = new Pen(Color.Violet, 6)}}
            };
        }

        private static MultiPolygon CreateMultiPolygon()
        {
            return new MultiPolygon
            {
                Polygons = new List<Polygon>
                {
                    new Polygon(new LinearRing(new[]
                    {
                        new Point(4000000, 3000000),
                        new Point(4000000, 2000000),
                        new Point(3000000, 2000000),
                        new Point(3000000, 3000000),
                        new Point(4000000, 3000000)
                    })),

                    new Polygon(new LinearRing(new[]
                    {
                        new Point(4000000, 5000000),
                        new Point(4000000, 4000000),
                        new Point(3000000, 4000000),
                        new Point(3000000, 5000000),
                        new Point(4000000, 5000000)
                    }))
                }
            };
        }

        private static Polygon CreatePolygon()
        {
            return new Polygon(new LinearRing(new[]
            {
                new Point(1000000, 1000000),
                new Point(1000000, -1000000),
                new Point(-1000000, -1000000),
                new Point(-1000000, 1000000),
                new Point(1000000, 1000000)
            }));
        }

        private static Polygon CreateAlternativePolygon()
        {
            return new Polygon(new LinearRing(new[]
            {
                new Point(3000000, 1000000),
                new Point(3000000, -1000000),
                new Point(2000000, -1000000),
                new Point(2000000, 1000000),
                new Point(3000000, 1000000)
            }));
        }

        private static LineString CreateLine()
        {
            var offsetX = -2000000;
            var offsetY = -2000000;
            var stepSize = -2000000;

            return new LineString(new[]
            {
                new Point(offsetX + stepSize,      offsetY + stepSize),
                new Point(offsetX + stepSize * 2,  offsetY + stepSize),
                new Point(offsetX + stepSize * 2,  offsetY + stepSize * 2),
                new Point(offsetX + stepSize * 3,  offsetY + stepSize * 2),
                new Point(offsetX + stepSize * 3,  offsetY + stepSize * 3)
            });
        }

        private static ILayer CreateInfoLayer(BoundingBox envelope)
        {
            return new Layer(InfoLayerName)
            {
                DataSource = PointsSample.CreateProviderWithRandomPoints(envelope, 25),
                Style = CreateSymbolStyle(),
                IncludeFeaturesInUserEvents = true
            };
        }

        private static ILayer CreateHoverLayer(BoundingBox envelope)
        {
            return new Layer(HoverLayerName)
            {
                DataSource = PointsSample.CreateProviderWithRandomPoints(envelope, 25),
                Style = CreateHoverSymbolStyle(),
                IncludeFeaturesInUserEvents = true
            };
        }

        private static SymbolStyle CreateHoverSymbolStyle()
        {
            return new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(new Color(251, 236, 215)),
                Outline = { Color = Color.Gray, Width = 1 }
            };
        }

        private static ILayer CreateHoverEventLayer(BoundingBox envelope)
        {
            var layer = new Layer(HoverEventLayerName)
            {
                DataSource = PointsSample.CreateProviderWithRandomPoints(envelope, 25),
                Style = CreateHoverEventSymbolStyle(),
                IncludeFeaturesInUserEvents = true
            };
            layer.HoveredOnce += (sender, args) =>
            {
                if (args.Feature != null)
                {
                    args.Feature.Styles.Clear();
                    args.Feature.Styles.Add(new SymbolStyle() {Outline = {Color = Color.Green, Width = 4}});
                    args.MapNeedsRefresh = true;
                }
            };
            layer.SingleTap += (sender, args) =>
            {
                if (args.Feature != null)
                {
                    var distance = args.ModifierShift ? 30.0 : -50.0;
                    (args.Feature.Geometry as Point).X += args.Viewport.Resolution * distance;
                    args.MapNeedsRefresh = true;
                }
            };

             IFeature _tmpFeature = null;
            layer.TouchStarted += (sender, args) =>
            {
                if (args.Feature != null)
                {
                    args.Handled = true;
                    _tmpFeature = args.Feature;
                }
            };

            layer.TouchMove += (sender, args) =>
            {
                if (_tmpFeature != null)
                {
                    _tmpFeature.Geometry = args.Viewport.ScreenToWorld(args.ScreenPoints.First());
                    args.MapNeedsRefresh = true;
                }
            };

            return layer;
        }

        private static SymbolStyle CreateHoverEventSymbolStyle()
        {
            return new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(new Color(34, 64, 115)),
                Outline = { Color = Color.Green, Width = 2 }
            };
        }

        private static SymbolStyle CreateSymbolStyle()
        {
            return new SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Brush(new Color(213, 234, 194)),
                Outline = { Color = Color.Gray, Width = 1 }
            };
        }
    }
}