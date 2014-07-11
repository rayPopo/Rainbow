﻿using Rainbow.App.GUI.Model;
using Rainbow.ImgLib.Formats;
using Rainbow.ImgLib.Formats.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Rainbow.App.GUI
{
    public partial class MainForm : Form
    {
        private enum TextureFormatMode { Format, Metadata, Unspecified };

        private void OpenImportTexture(TextureFormatMode mode)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = ConstructFilters(mode);
            var result = dialog.ShowDialog();

            if (result != DialogResult.OK)
                return;

            string name = dialog.FileName;

            try
            {
                using (Stream s = File.Open(name, FileMode.Open))
                    OpenImportStream(s, name, mode);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            FillListView(new string[] { name });
        }

        private void SaveExportTexture(TextureFormatMode mode)
        {
            if (texture == null)
                return;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = Path.GetFileNameWithoutExtension(filename);

            if (mode == TextureFormatMode.Unspecified)
                throw new Exception("Should not happen");

            dialog.Filter = serializer.Name +
                            (mode==TextureFormatMode.Format ? "|" : " metadata + editable data|") +
                            (mode == TextureFormatMode.Format ? serializer.PreferredFormatExtension : serializer.PreferredMetadataExtension);

            var result = dialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            try
            {
                using (Stream s = File.Open(dialog.FileName, FileMode.Create))
                {
                    if (mode==TextureFormatMode.Format)
                        serializer.Save(texture, s);
                    else
                        serializer.Export(texture, s, Path.GetDirectoryName(dialog.FileName), Path.GetFileNameWithoutExtension(dialog.FileName));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void OpenImportFolder(TextureFormatMode mode)
        {
            if (mode == TextureFormatMode.Unspecified)
                throw new Exception("Should not happen");
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            var result = dialog.ShowDialog();

            if (result != DialogResult.OK)
                return;

            string path = dialog.SelectedPath;

            IEnumerable<string> extensions = TextureFormatSerializerProvider.RegisteredSerializers.Select(s => mode == TextureFormatMode.Format ? s.PreferredFormatExtension : s.PreferredMetadataExtension);
            extensions = extensions.OrderBy(s => s);
            var files = Directory.GetFiles(path, "*.*").Where(s => extensions.Contains(Path.GetExtension(s)));

            FillListView(files);
        }

        private void OpenImportStream(Stream stream, string fullPath,TextureFormatMode mode)
        {

            TextureFormatSerializer curSerializer = null;
            curSerializer = TextureFormatSerializerProvider.FromStream(stream);

            if (curSerializer == null)
            {
                MessageBox.Show("Unsupported file format!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

			switch(mode)
            {
				case TextureFormatMode.Format:
                    SetTexture(curSerializer.Open(stream));
                    break;
                case TextureFormatMode.Metadata:
                    SetTexture(curSerializer.Import(stream, Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath)));
                    break;
                default:
					if(curSerializer.IsValidFormat(stream))
                    {
                        SetTexture(curSerializer.Open(stream));
                    }else
                        SetTexture(curSerializer.Import(stream, Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath)));
                    break;
            }
            
            SetFilename(Path.GetFileName(fullPath));
            serializer = curSerializer;

        }

        private string ConstructFilters(TextureFormatMode mode)
        {
            if (mode == TextureFormatMode.Unspecified)
                throw new Exception("Should not happen");

            StringBuilder builder = new StringBuilder();

            IEnumerable<TextureFormatSerializer> ordered = TextureFormatSerializerProvider.RegisteredSerializers.OrderBy(s => s.Name);

            StringBuilder allFormatsBuilder = new StringBuilder();
            allFormatsBuilder.Append("All supported " + (mode == TextureFormatMode.Format ? "formats|" : "metadata formats|"));

            foreach (var serializer in ordered)
            {
                string ext = mode==TextureFormatMode.Format ? serializer.PreferredFormatExtension :
                                      serializer.PreferredMetadataExtension;

                allFormatsBuilder.AppendFormat("*{0};", ext);
                builder.AppendFormat("{0}|*{1}|", mode == TextureFormatMode.Format ? serializer.Name :
																				 serializer.Name + " metadata",
                                     ext);
            }

            string f = allFormatsBuilder.Remove(allFormatsBuilder.Length - 1, 1).
                                     Append('|').Append(builder).
                                     Append("All files|*.*").ToString();
            return f;

        }

        private void SetTexture(TextureFormat tex)
        {
            texture = tex;
            propertyGrid.SelectedObject = PropertyGridObjectFactory.Create(texture);

            transparentPictureBox1.SetTexture(texture);
        }

        private void SetFilename(string name)
        {
            this.filename = Path.GetFileName(name);
            this.Text = filename + " - " + Application.ProductName;
        }

        private void FillListView(IEnumerable<string> files)
        {
            int i = 0;
            listView.Items.Clear();
            foreach (string file in files)
            {
                TextureFormatProxy proxy = new FileTextureFormatProxy(file);
                ListViewItem item = new ListViewItem(new string[] { (i++).ToString(), proxy.Name, proxy.Size.ToString() });
                item.Tag = proxy;
                listView.Items.Add(item);
            }
        }
    }
}