/*
 *  GameMap.cs
 *
 *  Copyright (C) 2010-2012 Kyle Olson, kyle@kyleolson.com
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU General Public License
 *  as published by the Free Software Foundation; either version 2
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using CombatManager;
using System.Xml.Serialization;
using System.IO;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace CombatManager.Maps
{
    public class GameMap : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        Point cellOrigin;

        double scale;
        double tableScale;

        double cellSizeWidth;
        double cellSizeHeight;

        String sourceFile;

        BitmapImage image;

        String name;

        BitArray fog;

        bool showGrid = true;
        Color gridColor = Color.FromArgb(255, 255, 255, 255);
        

        int id;

        Dictionary<int, List<Marker>> markers = new Dictionary<int, List<Marker>>();

        bool canSave;

        public delegate void MapEvent(GameMap map);
        public event MapEvent FogOrMarkerChanged;

        public GameMap()
        {

            CellSize = new Size(100.0D, 100.0D);
            CellOrigin = new Point(0, 0);
            Scale = 1D;
            TableScale = 1D;
        

        }

        public GameMap(int id, String filename) : this()
        {
            this.Id = id;
            SourceFile = filename;


            FileInfo info = new FileInfo(filename);
            Name = info.Name.Substring(0, info.Name.Length - info.Extension.Length);
        }


        public double Scale
        {
            get { return scale; }
            set
            {
                double setValue = value.Clamp(.01, 20);

                if (scale != setValue)
                {
                    scale = setValue;
                    NotifyAndSave("Scale");
                }
            }
        }

        public double TableScale
        {
            get { return tableScale; }
            set
            {
                double setValue = value.Clamp(.01, 20);

                if (tableScale != setValue)
                {
                    tableScale = setValue;
                    NotifyAndSave("TableScale");
                }
            }
        }


        public Size CellSize
        {
            get { return new Size(cellSizeWidth, cellSizeHeight); }
            set
            {
                CellSizeWidth = value.Width;
                CellSizeHeight = value.Height;
            }
        }

        public double CellSizeWidth
        {
            get { return cellSizeWidth; }
            set
            {

                double setValue = value.Clamp(1, 2000);


                if (cellSizeWidth != setValue)
                {
                    cellSizeWidth = setValue;
                    Notify("CellSize");
                    NotifyAndSave("CellSizeWidth");
                }
            }
        }
        public double CellSizeHeight
        {
            get { return cellSizeHeight; }
            set
            {
                double setValue = value.Clamp(1, 2000);
                if (cellSizeHeight != setValue)
                {
                    cellSizeHeight = setValue;
                    Notify("CellSize");
                    NotifyAndSave("CellSizeHeight");
                }
            }
        }

        public String SourceFile
        {
            get { return sourceFile; }
            set
            {
                if (sourceFile != value)
                {
                    sourceFile = value;
                    NotifyAndSave("SourceFile");
                }
            }
        }


        public bool this[int x, int y]
        {
            get
            {


               return Fog[y * GridMaxWidth + x];
                

            }
            set
            {
                Fog[y * GridMaxWidth + x] = value;
            }
        }

        
        
        [XmlIgnore, JsonIgnore]
        public BitmapImage Image
        {
            get
            {
                if (image == null && sourceFile != null)
                {
                   image = new BitmapImage(new Uri(sourceFile));
                }
                return image;
            }
            set
            {
                if (image != value)
                {
                    image = value;

                    NotifyAndSave("Image");
                    
                }
            }
        }
        public Point CellOrigin
        {
            get { return cellOrigin; }
            set
            {
                if (cellOrigin != value)
                {
                    cellOrigin = value;
                    NotifyAndSave("CellOrigin");
                }
            }
        }

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (name != value)
                {
                    name = value;
                    Notify("Name");
                }
            }
        }

        private void Notify(string prop)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        private void NotifyAndSave(string prop)
        {
            if (this.PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
            SaveMap(false);
        }


        [XmlIgnore, JsonIgnore]
        public BitArray Fog
        {
            get
            {
                if (fog == null)
                {
                    fog = new BitArray(GridMaxWidth * GridMaxHeight);
                }
                return fog;
            }
            set
            {
                fog = value;
                Notify("Fog");
            }
        }

        public const int GridMaxWidth = 250;
        public const int GridMaxHeight = 250;

        public int Id
        {
            get
            {
                return id;
            }

            set
            {
                if (id != value)
                {
                    id = value;
                    Notify("Id");
                }
            }
        }


        [XmlIgnore, JsonIgnore]
        public int CellsWidth
        {
            get
            {
                return ((int)Math.Ceiling(((double)Image.Width) / CellSizeWidth) + 1).Max(GridMaxWidth);
            }
        }

        [XmlIgnore, JsonIgnore]
        public int CellsHeight
        {
            get
            {
                return ((int)Math.Ceiling(((double)Image.Height) / CellSizeHeight) + 1).Max(GridMaxHeight);
            }
        }

        [XmlIgnore, JsonIgnore]
        public Size ActualCellSize
        {
            get
            {

                double cellActualWidth = CellSizeWidth * Scale;
                double cellActualHeight = CellSizeHeight * Scale;
                return new Size(cellActualWidth, cellActualHeight);
            }
        }

        [XmlIgnore, JsonIgnore]
        public Size TableCellSize
        {
            get
            {

                double cellActualWidth = CellSizeWidth * TableScale;
                double cellActualHeight = CellSizeHeight * TableScale;
                return new Size(cellActualWidth, cellActualHeight);
            }
        }

        [XmlIgnore, JsonIgnore]
        public Point GridOrigin
        {
            get
            {
                double xDistance = Math.Ceiling(cellOrigin.X / CellSizeWidth);
                double yDistance = Math.Ceiling(cellOrigin.Y / CellSizeHeight);

                Point p = new Point();
                p.X = cellOrigin.X - xDistance * CellSizeWidth;
                p.Y = cellOrigin.Y - yDistance * CellSizeHeight;

                return p;

            }
        }

        [XmlIgnore, JsonIgnore]
        public Point ActualGridOrigin
        {
            get
            {
                return GridOrigin.Multiply(scale);
            }
        }

        [XmlIgnore, JsonIgnore]
        public Point TableGridOrigin
        {
            get
            {
                return GridOrigin.Multiply(tableScale);
            }
        }

        [XmlIgnore, JsonIgnore]
        public Dictionary<int, List<Marker>> Markers
        {
            get
            {
                return markers;
            }
        }


        public class ExportMarkerItem
        {
            int index;
            List<Marker> markers = new List<Marker>();

            public int Index
            {
                get
                {
                    return index;
                }

                set
                {
                    index = value;
                }
            }

            public List<Marker> Markers
            {
                get
                {
                    return markers;
                }

                set
                {
                    markers = value;
                }
            }
        }

        public ObservableCollection<ExportMarkerItem> ExportMarkers
        {
            get
            {
               
                ObservableCollection<ExportMarkerItem> list = new ObservableCollection<ExportMarkerItem>();
                foreach (KeyValuePair<int, List<Marker>> pair in markers)
                {
                    ExportMarkerItem t = new ExportMarkerItem() { Index = pair.Key, Markers = pair.Value };
                    list.Add(t);

                }
                list.CollectionChanged += MarkerListChanged;
                return list;
            }
            set
            {
                markers = new Dictionary<int, List<Marker>>();
                foreach (ExportMarkerItem t in value)
                {
                    markers[t.Index] = t.Markers;
                }
            }
        }

        private void MarkerListChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (markers == null)
            {
                markers = new Dictionary<int, List<Marker>>();
            }
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (ExportMarkerItem m in e.NewItems)
                {
                    markers[m.Index] = m.Markers;
                }
            }
        }

        public bool ShowGrid
        {
            get
            {
                return showGrid;
            }

            set
            {
                if (showGrid != value)
                {
                    showGrid = value;
                    Notify("ShowGrid");
                }
            }
        }

        [XmlIgnore, JsonIgnore]
        public bool CanSave
        {
            get
            {
                return canSave;
            }

            set
            {
                canSave = value;
            }
        }

        public Color GridColor
        {
            get => gridColor;
            set
            {
                if (gridColor != value)
                {
                    gridColor = value;
                    Notify("GridColor");
                }
            }
        }

        public void SetMarker(MapCell cell, Marker marker)
        {
            SetMarker(cell.X, cell.Y, marker);
        }

        public void SetMarker(int x, int y, Marker marker)
        {
            int index = CellIndex(x, y);
            List<Marker> list;

            if (markers.ContainsKey(index))
            {
                list = markers[CellIndex(x, y)];
            }
            else 
            {
                list = new List<Marker>();
                markers[CellIndex(x, y)] = list;
            }
            list.Clear();
            list.Add(marker);
        
        }

        public void DeleteAllMarkers(MapCell cell)
        {
            DeleteAllMarkers(cell.X, cell.Y);
        }

        public void DeleteAllMarkers(int x, int y)
        {
            int index = CellIndex(x, y);

            if (markers.ContainsKey(index))
            {
                markers.Remove(index);
            }
        }

        public int CellIndex(GameMap.MapCell cell)
        {
            return CellIndex(cell.X, cell.Y);
        }

        public int CellIndex(int x, int y)
        {
            return x + y * GridMaxWidth;
        }

        public MapCell IndexToCell(int index)
        {
            return new MapCell() { X = index % GridMaxWidth, Y = index / GridMaxWidth };
        }

        public List<Marker> GetMarkers(GameMap.MapCell cell)
        {
            if (markers.ContainsKey(CellIndex(cell)))
            {
                return markers[CellIndex(cell)];
            }
            return new List<Marker>();
        }

        private List<Marker> GetEditableList(int id)
        {

            if (markers.ContainsKey(id))
            {
                return markers[id];

            }
            else
            {
                List<Marker> list = new List<Marker>();
                markers[id] = list;
                return list;
            }
        }

        public bool CellHasMarkers(GameMap.MapCell cell)
        {
            return GetMarkers(cell).Count() > 0;
        }



        public struct MapCell
        {
            public int X;
            public int Y;
            
            public MapCell(int x, int y)
            {
                X = x;
                Y = y;
            }
        }



        public enum MarkerStyle
        {
            Square = 0,
            Diamond = 1,
            Circle = 2,
            Star = 3,
            Target = 4
        }



        static string CreateFogFileName(int id)
        {
            return "GameMap_" + id + ".fog";
        }
        static string CreateFileName(int id)
        {
            return "GameMap_" + id + ".xml";
        }


        public static GameMap LoadMap(int id)
        {
            GameMap map= (GameMap)XmlLoader<GameMap>.Load(CreateFileName(id), true);

            FileInfo info = new FileInfo(XmlLoader<GameMap>.SaveFileName(CreateFogFileName(id), true));

            if (info.Exists)
            {
                using (FileStream stream = info.OpenRead())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);

                        map.fog = new BitArray(memoryStream.ToArray());
                    }

                    stream.Close();
                }
            }

            return map;
            

        }


        public void SaveMap(bool saveFog)
        {
            if (CanSave)
            {
                XmlLoader<GameMap>.Save(this, CreateFileName(id), true);

                if (saveFog)
                {
                    FileInfo info = new FileInfo(XmlLoader<GameMap>.SaveFileName(CreateFogFileName(id), true));
                    using (FileStream stream = info.OpenWrite())
                    {
                        byte[] byteArray = new byte[(int)Math.Ceiling((double)Fog.Length / 8)];
                        Fog.CopyTo(byteArray, 0);
                        stream.Write(byteArray, 0, byteArray.Length);

                        stream.Close();
                    }
                }
            }
        }


        public static void Delete(int id)
        {
            XmlLoader<GameMap>.Delete(CreateFileName(id), true);
            FileInfo info = new FileInfo(XmlLoader<GameMap>.SaveFileName(CreateFogFileName(id), true));
            if (info.Exists)
            {
                info.Delete();
            }
        }

        public void FireFogOrMarkerChanged()
        {
            FogOrMarkerChanged?.Invoke(this);
        }

        public class Marker  : INotifyPropertyChanged
        {


            public event PropertyChangedEventHandler PropertyChanged;



            private void Notify(string prop)
            {
                if (this.PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
                }
            }

            MarkerStyle style;
            Color color;
            String data;
            String dataType;

            public MarkerStyle Style
            {
                get
                {
                    return style;
                }

                set
                {
                    style = value;
                    Notify("Style");
                }
            }

            public Color Color
            {
                get
                {
                    return color;
                }

                set
                {
                    color = value;
                    Notify("Color");
                }
            }

            public string Data
            {
                get
                {
                    return data;
                }

                set
                {
                    data = value;
                    Notify("Data");
                }
            }

            public string DataType
            {
                get
                {
                    return dataType;
                }

                set
                {
                    dataType = value;
                    Notify("DataType");
                }
            }
        }

    }
}
