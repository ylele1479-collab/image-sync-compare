using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
[assembly:System.Reflection.AssemblyVersion("1.2.0.0")]
public class Pair { public string A,B; public Pair(string a,string b){A=a;B=b;} }
public class Loaded : IDisposable { public Bitmap A,B; public long Bytes {get{return ((long)A.Width*A.Height+(long)B.Width*B.Height)*4;}} public void Dispose(){A.Dispose();B.Dispose();} }
public class Canvas : Control {
 public event Action ZoomChanged; public Bitmap Image; public float Zoom=1; public PointF Pan; Point last; bool dragging;
 public Canvas(){DoubleBuffered=true; BackColor=Color.FromArgb(22,22,24); Dock=DockStyle.Fill; TabStop=true; Resize+=delegate{Invalidate();}; MouseWheel+=delegate(object s,MouseEventArgs e){Zoom=Math.Max(.1f,Math.Min(16,Zoom*(e.Delta>0?1.2f:1/1.2f)));Invalidate();if(ZoomChanged!=null)ZoomChanged();}; MouseDown+=delegate(object s,MouseEventArgs e){Focus(); dragging=true;last=e.Location;Capture=true;}; MouseUp+=delegate{dragging=false;Capture=false;}; MouseMove+=delegate(object s,MouseEventArgs e){if(dragging){Pan=new PointF(Pan.X+e.X-last.X,Pan.Y+e.Y-last.Y);last=e.Location;Invalidate();}}; }
 public void Reset(){Zoom=1;Pan=PointF.Empty;Invalidate();if(ZoomChanged!=null)ZoomChanged();}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Image==null){TextRenderer.DrawText(e.Graphics,"正在加载图片…",Font,ClientRectangle,Color.Silver,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);return;} float scale=Math.Min((float)Width/Image.Width,(float)Height/Image.Height)*Zoom;float w=Image.Width*scale,h=Image.Height*scale;e.Graphics.InterpolationMode=InterpolationMode.HighQualityBilinear;e.Graphics.DrawImage(Image,(Width-w)/2+Pan.X,(Height-h)/2+Pan.Y,w,h);}
}
public partial class CompareForm : Form {
 public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
 public List<Pair> Pairs=new List<Pair>(); public int Index=0; public bool SideB=false;
 Canvas canvas=new Canvas(); Label status=new Label(); ToolStrip tools=new ToolStrip(); ToolStripLabel folders=new ToolStripLabel();
 Dictionary<int,Loaded> cache=new Dictionary<int,Loaded>(); LinkedList<int> lru=new LinkedList<int>(); bool busy=false; Loaded detail=null; int detailIndex=-1; int direction=1; int generation=0; bool closing=false; string folderA="",folderB=""; System.Windows.Forms.Timer blink=new System.Windows.Forms.Timer();
 [DllImport("shlwapi.dll",CharSet=CharSet.Unicode)] static extern int StrCmpLogicalW(string a,string b);
 public static string[] Files(string p){string[] fs=Directory.GetFiles(p).Where(f=>new[]{".jpg",".jpeg",".png",".bmp",".gif",".tif",".tiff"}.Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();Array.Sort(fs,(a,b)=>StrCmpLogicalW(Path.GetFileName(a),Path.GetFileName(b)));return fs;}
 public static List<Pair> MakePairs(string a,string b){var aa=Files(a);var bb=Files(b);var result=new List<Pair>();for(int i=0;i<Math.Min(aa.Length,bb.Length);i++)result.Add(new Pair(aa[i],bb[i]));return result;}
 ToolStripButton Button(string text,Action action){var b=new ToolStripButton(text);b.Click+=delegate{action();canvas.Focus();};tools.Items.Add(b);return b;}
 public CompareForm(){Text="图片同步对比 1.2";Width=1280;Height=850;MinimumSize=new Size(800,500);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);WindowState=FormWindowState.Maximized;
 tools.GripStyle=ToolStripGripStyle.Hidden;tools.Padding=new Padding(8);tools.Font=Font;tools.Dock=DockStyle.Top;
 Button("① 导入文件夹 A",()=>Pick(false));Button("② 导入文件夹 B",()=>Pick(true));tools.Items.Add(new ToolStripSeparator());Button("◀ 上一组 A / ←",()=>Navigate(-1));Button("下一组 D / → ▶",()=>Navigate(1));Button("切换原图 / 修改图 [空格]",Toggle);Button("适应窗口 [0]",()=>canvas.Reset());Button("闪烁 [F]",()=>{blink.Enabled=!blink.Enabled;UpdateStatus();});tools.Items.Add(folders);
 status.Dock=DockStyle.Bottom;status.Height=56;status.Padding=new Padding(10,5,10,4);status.ForeColor=Color.White;status.BackColor=Color.FromArgb(40,40,44);status.AutoEllipsis=true;
 SetupDirectory(); canvas.ZoomChanged+=delegate{Display();Pump();}; Controls.Add(contentSplit);Controls.Add(status);Controls.Add(tools);blink.Interval=500;blink.Tick+=delegate{Toggle();};
 Shown+=delegate{try{string cfg=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"folders.txt");var lines=File.Exists(cfg)?File.ReadAllLines(cfg):new string[0];folderA=lines.Length>=2&&Directory.Exists(lines[0])?lines[0]:Path.Combine(Root,"原图");folderB=lines.Length>=2&&Directory.Exists(lines[1])?lines[1]:Path.Combine(Root,"竞品图");if(Directory.Exists(folderA)&&Directory.Exists(folderB)){Import();var args=Environment.GetCommandLineArgs();if(args.Length>1){int resume;if(int.TryParse(args[1],out resume)){Index=Math.Max(0,Math.Min(Pairs.Count-1,resume));SideB=args.Length>2&&args[2]=="B";ShowPair();}}}else UpdateStatus();canvas.Focus();}catch(Exception ex){MessageBox.Show(ex.Message,"读取失败");}};
 FormClosing+=delegate{closing=true;generation++;blink.Stop();canvas.Image=null;if(detail!=null){detail.Dispose();detail=null;}foreach(var p in cache.Values)p.Dispose();cache.Clear();}; }
 void Pick(bool second){using(var d=new FolderBrowserDialog()){d.Description=second?"选择文件夹 B（修改图）":"选择文件夹 A（原图）";d.SelectedPath=second?folderB:folderA;if(d.ShowDialog(this)!=DialogResult.OK)return;if(second)folderB=d.SelectedPath;else folderA=d.SelectedPath;}if(Directory.Exists(folderA)&&Directory.Exists(folderB))Import();else UpdateStatus();}
 public void Import(){generation++;blink.Stop();canvas.Image=null;if(detail!=null){detail.Dispose();detail=null;detailIndex=-1;}foreach(var v in cache.Values)v.Dispose();cache.Clear();lru.Clear();Pairs=MakePairs(folderA,folderB);Index=0;SideB=false;RebuildDirectory();canvas.Reset();folders.Text="  "+Pairs.Count+" 组 · 按文件名自然顺序配对";try{File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"folders.txt"),new[]{folderA,folderB});}catch{} int ca=Files(folderA).Length,cb=Files(folderB).Length;if(ca!=cb)MessageBox.Show("两个文件夹图片数量不同：A="+ca+"，B="+cb+"。仅配对前 "+Pairs.Count+" 张，请注意检查文件名。","数量提醒");ShowPair();}
 public void Navigate(int delta){if(Pairs.Count==0)return;int next=Math.Max(0,Math.Min(Pairs.Count-1,Index+delta));if(next==Index)return;direction=delta>=0?1:-1;Index=next;canvas.Reset();ShowPair();}
 public void Toggle(){if(Pairs.Count==0)return;SideB=!SideB;Display();}
 protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Space){Toggle();return true;}if(key==Keys.Right||key==Keys.D){Navigate(1);return true;}if(key==Keys.Left||key==Keys.A){Navigate(-1);return true;}if(key==Keys.D0||key==Keys.NumPad0){canvas.Reset();return true;}if(key==Keys.F){blink.Enabled=!blink.Enabled;UpdateStatus();return true;}if(key==Keys.Home){Index=0;canvas.Reset();ShowPair();return true;}if(key==Keys.End&&Pairs.Count>0){Index=Pairs.Count-1;canvas.Reset();ShowPair();return true;}return base.ProcessCmdKey(ref msg,key);}
 void Touch(int n){lru.Remove(n);lru.AddLast(n);while(cache.Count>24||cache.Values.Sum(v=>v.Bytes)>384L*1024*1024){var node=lru.First;while(node!=null&&node.Value==Index)node=node.Next;if(node==null)break;int old=node.Value;lru.Remove(node);cache[old].Dispose();cache.Remove(old);}}
 public static Bitmap Decode(string file){using(var original=System.Drawing.Image.FromFile(file)){try{if(original.PropertyIdList.Contains(0x112)){int o=original.GetPropertyItem(0x112).Value[0];RotateFlipType flip=RotateFlipType.RotateNoneFlipNone;switch(o){case 2:flip=RotateFlipType.RotateNoneFlipX;break;case 3:flip=RotateFlipType.Rotate180FlipNone;break;case 4:flip=RotateFlipType.Rotate180FlipX;break;case 5:flip=RotateFlipType.Rotate90FlipX;break;case 6:flip=RotateFlipType.Rotate90FlipNone;break;case 7:flip=RotateFlipType.Rotate270FlipX;break;case 8:flip=RotateFlipType.Rotate270FlipNone;break;}original.RotateFlip(flip);}}catch{}return new Bitmap(original);}}
 static Loaded Read(Pair p){var a=Decode(p.A);try{return new Loaded{A=a,B=Decode(p.B)};}catch{a.Dispose();throw;}}
 void ShowPair(){Display();Pump();}
 static Loaded ReadPreview(Pair p){var a=FastPreview.Load(p.A);try{return new Loaded{A=a,B=FastPreview.Load(p.B)};}catch{a.Dispose();throw;}}
 async void Pump(){if(busy||closing||Pairs.Count==0)return;busy=true;try{while(!closing&&Pairs.Count>0){int n=Index;bool full=false;if(cache.ContainsKey(n)){if(canvas.Zoom>1.01f&&(detail==null||detailIndex!=n))full=true;else{n=-1;for(int j=1;j<=10;j++){int next=Index+direction*j;if(next>=0&&next<Pairs.Count&&!cache.ContainsKey(next)){n=next;break;}}if(n<0){for(int j=1;j<=2;j++){int next=Index-direction*j;if(next>=0&&next<Pairs.Count&&!cache.ContainsKey(next)){n=next;break;}}}if(n<0)break;}}
 int session=generation;Pair pair=Pairs[n];Loaded loaded=null;try{loaded=await Task.Run(()=>full?Read(pair):ReadPreview(pair));}catch(Exception ex){if(session==generation&&n==Index){status.Text="图片读取失败："+ex.Message;break;}else break;}
 if(closing||session!=generation){loaded.Dispose();continue;}
 if(full){if(n==Index&&canvas.Zoom>1.01f){canvas.Image=null;if(detail!=null)detail.Dispose();detail=loaded;detailIndex=n;}else loaded.Dispose();}else{if(!cache.ContainsKey(n)){cache[n]=loaded;Touch(n);}else loaded.Dispose();}Display();
 }}finally{busy=false;}}
 void Display(){SyncDirectory();Loaded p;bool hd=canvas.Zoom>1.01f&&detail!=null&&detailIndex==Index;canvas.Image=hd?(SideB?detail.B:detail.A):(cache.TryGetValue(Index,out p)?(SideB?p.B:p.A):null);canvas.Invalidate();UpdateStatus();}
 void UpdateStatus(){if(Pairs.Count==0){status.Text="请分别导入两个文件夹（JPG / PNG / BMP / TIFF）。";return;}var p=Pairs[Index];string side=SideB?"B 修改图":"A 原图";string loading=canvas.Image==null?" · 读取中…":"";string dimensions=canvas.Image==null?"": " · "+canvas.Image.Width+"×"+canvas.Image.Height+(canvas.Zoom>1.01f?(detail!=null&&detailIndex==Index?" 原始高清":" 高清加载中…"):" 快速预览（放大自动高清）");Text="图片同步对比 1.2 — "+(Index+1)+" / "+Pairs.Count+" — "+side+loading;status.Text=(Index+1)+" / "+Pairs.Count+"   【"+side+"】 "+Path.GetFileName(SideB?p.B:p.A)+dimensions+loading+(blink.Enabled?" · 闪烁中":"")+"\n空格：同位置切换  |  ← / A 上一组  → / D 下一组  |  滚轮缩放 · 拖动平移 · 0 复位  |  A: "+new DirectoryInfo(folderA).Name+"  B: "+new DirectoryInfo(folderB).Name;}
 [STAThread] public static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new CompareForm());}
}




