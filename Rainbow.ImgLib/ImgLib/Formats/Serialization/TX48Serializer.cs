﻿//Copyright (C) 2014+ Marco (Phoenix) Calautti.

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, version 2.0.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License 2.0 for more details.

//A copy of the GPL 2.0 should have been included with the program.
//If not, see http://www.gnu.org/licenses/

//Official repository and contact information can be found at
//http://github.com/marco-calautti/Rainbow

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace Rainbow.ImgLib.Formats.Serialization
{
    public class TX48Serializer : TextureFormatSerializer
    {
        private const string MAGIC = "TX48";
        public string Name
        {
            get { return TX48Texture.NAME; }
        }

        public string PreferredFormatExtension
        {
            get { return ".tx48"; }
        }

        public bool IsValidFormat(System.IO.Stream inputFormat)
        {
            long oldPos = inputFormat.Position;
            try
            {
                char[] magic = new BinaryReader(inputFormat).ReadChars(MAGIC.Length);
                return new string(magic) == MAGIC;
            }
            finally
            {
                inputFormat.Position = oldPos;
            }
        }

        public bool IsValidMetadataFormat(Metadata.MetadataReader metadata)
        {
            try
            {
                metadata.EnterSection("TX48Texture");
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                metadata.Rewind();
            }
            return true;
        }

        public TextureFormat Open(System.IO.Stream formatData)
        {


            BinaryReader reader = new BinaryReader(formatData);

            try
            {

                char[] magic = reader.ReadChars(MAGIC.Length);
                if (new string(magic) != MAGIC)
                    throw new TextureFormatException("Not a valid TX48 Texture!");

                int bpp = reader.ReadInt32();
                if (bpp != 0 && bpp != 1)
                    throw new TextureFormatException("Illegal Bit per pixel value!");

                bpp = (bpp + 1) * 4;

                int width = reader.ReadInt32();
                int height = reader.ReadInt32();
                int paletteOffset = reader.ReadInt32();
                if (paletteOffset != 0x40)
                    throw new TextureFormatException("TX48 Header is wrong!");

                int paletteSize = reader.ReadInt32();
                int imageOffset = reader.ReadInt32();
                int imageSize = reader.ReadInt32();
                reader.BaseStream.Position += 0x20;

                byte[] paletteData = reader.ReadBytes(paletteSize);
                byte[] imageData = reader.ReadBytes(imageSize);

                return new TX48Texture(imageData, paletteData, width, height, bpp);

            }
            catch (Exception e)
            {
                if (e is TextureFormatException)
                    throw e;
                throw new TextureFormatException(e.Message, e);
            }
        }

        public void Save(TextureFormat txt, System.IO.Stream outFormatData)
        {
            TX48Texture texture = txt as TX48Texture;
            if (texture == null)
                throw new TextureFormatException("Not a valid TX48Texture!");

            BinaryWriter writer = new BinaryWriter(outFormatData);

            IList<byte[]> imagesData = texture.GetImagesData();
            IList<byte[]> palettesData = texture.GetPaletteData();
            int[] widths = texture.GetWidths();
            int[] heights = texture.GetHeights();
            int[] bpps = texture.GetBpps();


            byte[] pal = palettesData[0];
            byte[] img = imagesData[0];

            writer.Write(MAGIC.ToCharArray());
            writer.Write(bpps[0] / 4 - 1);

            writer.Write(widths[0]);
            writer.Write(heights[0]);

            writer.Write(0x40);
            writer.Write(pal.Length);

            writer.Write(0x40 + pal.Length);
            writer.Write(img.Length);

            for (int j = 0; j < 8; j++)
                writer.Write(0);

            writer.Write(pal);
            writer.Write(img);
        }

        public void Export(TextureFormat txt, Metadata.MetadataWriter metadata, string directory, string basename)
        {
            TX48Texture texture = txt as TX48Texture;
            if (texture == null)
                throw new TextureFormatException("Not a valid TX48Texture!");
            try
            {
                metadata.BeginSection("TX48Texture");
                metadata.PutAttribute("Basename", basename);

                metadata.BeginSection("TX48Segment");
                metadata.Put("Bpp", texture.Bpp);
                metadata.EndSection();

                texture.GetImage().Save(Path.Combine(directory, basename + ".png"));
                metadata.EndSection();

            }
            catch (Exception e)
            {
                throw new TextureFormatException(e.Message, e);
            }
        }

        public TextureFormat Import(Metadata.MetadataReader metadata, string directory, string bname)
        {
            metadata.EnterSection("TX48Texture");
            string basename = metadata.GetAttributeString("Basename");

            metadata.EnterSection("TX48Segment");
            int bpp = metadata.GetInt("Bpp");
            Image img = Image.FromFile(Path.Combine(directory, basename + ".png"));
            metadata.ExitSection();

            metadata.ExitSection();

            return new TX48Texture(img, bpp);
        }
    }
}