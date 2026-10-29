using System.Linq;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
public static class FastPreview {
 public static readonly string CacheDirectory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preview-cache-v1");
 public static string CachePath(string file){var info=new FileInfo(file);string key=Path.GetFullPath(file).ToUpperInvariant()+"|"+info.Length+"|"+info.LastWriteTimeUtc.Ticks+"|1920";using(var sha=SHA256.Create()){return Path.Combine(CacheDirectory,BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(key))).Replace("-","")+(new[]{".jpg",".jpeg"}.Contains(Path.GetExtension(file).ToLowerInvariant())?".bmp":".png"));}}
 public static Bitmap Load(string file){string cached=CachePath(file);if(File.Exists(cached)){try{return CopyBitmap(cached);}catch{}}
 Bitmap result=Decode(file);try{Directory.CreateDirectory(CacheDirectory);string temp=cached+"."+Guid.NewGuid().ToString("N")+".tmp";try{result.Save(temp,Path.GetExtension(cached)==".png"?ImageFormat.Png:ImageFormat.Bmp);if(!File.Exists(cached))File.Move(temp,cached);}finally{if(File.Exists(temp))File.Delete(temp);}}catch{}return result;
 }
 static Bitmap CopyBitmap(string file){using(var image=System.Drawing.Image.FromFile(file)){var result=new Bitmap(image.Width,image.Height,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);using(var g=Graphics.FromImage(result))g.DrawImageUnscaled(image,0,0);return result;}}
 public static Bitmap Decode(string file){using(var stream=File.OpenRead(file)){int orientation=1,width=0,height=0;try{var decoder=BitmapDecoder.Create(stream,BitmapCreateOptions.DelayCreation,BitmapCacheOption.None);var frame=decoder.Frames[0];width=frame.PixelWidth;height=frame.PixelHeight;var meta=frame.Metadata as BitmapMetadata;if(meta!=null){object value=null;foreach(string query in new[]{"/app1/ifd/{ushort=274}","/ifd/{ushort=274}"}){try{value=meta.GetQuery(query);}catch{}if(value!=null){orientation=Convert.ToInt32(value);break;}}}}catch{}stream.Position=0;
 var source=new BitmapImage();source.BeginInit();source.CacheOption=BitmapCacheOption.OnLoad;source.StreamSource=stream;if(Math.Max(width,height)>1920){if(width>=height)source.DecodePixelWidth=1920;else source.DecodePixelHeight=1920;}source.EndInit();source.Freeze();var pixels=new FormatConvertedBitmap(source,PixelFormats.Pbgra32,null,0);var result=new Bitmap(pixels.PixelWidth,pixels.PixelHeight,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);var data=result.LockBits(new Rectangle(0,0,result.Width,result.Height),ImageLockMode.WriteOnly,result.PixelFormat);try{pixels.CopyPixels(Int32Rect.Empty,data.Scan0,Math.Abs(data.Stride)*data.Height,data.Stride);}finally{result.UnlockBits(data);}RotateFlipType flip=RotateFlipType.RotateNoneFlipNone;switch(orientation){case 2:flip=RotateFlipType.RotateNoneFlipX;break;case 3:flip=RotateFlipType.Rotate180FlipNone;break;case 4:flip=RotateFlipType.Rotate180FlipX;break;case 5:flip=RotateFlipType.Rotate90FlipX;break;case 6:flip=RotateFlipType.Rotate90FlipNone;break;case 7:flip=RotateFlipType.Rotate270FlipX;break;case 8:flip=RotateFlipType.Rotate270FlipNone;break;}result.RotateFlip(flip);return result;
 }}
}

