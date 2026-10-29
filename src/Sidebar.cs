using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

public partial class CompareForm {
 SplitContainer contentSplit;
 ListView directory;
 Label directoryTitle;
 bool syncingDirectory;

 void SetupDirectory(){
  contentSplit=new SplitContainer();
  contentSplit.Dock=DockStyle.Fill;
  contentSplit.Size=new Size(1200,700);
  contentSplit.Panel1MinSize=220;
  contentSplit.Panel2MinSize=350;
  contentSplit.SplitterDistance=360;
  contentSplit.SplitterWidth=6;
  contentSplit.BackColor=Color.FromArgb(65,65,70);
  directory=new ListView();
  directory.AccessibleName="图片目录";
  directory.Dock=DockStyle.Fill;
  directory.View=View.Details;
  directory.FullRowSelect=true;
  directory.MultiSelect=false;
  directory.HideSelection=false;
  directory.VirtualMode=true;
  directory.Font=new Font("Microsoft YaHei UI",9);
  directory.BackColor=Color.FromArgb(34,34,38);
  directory.ForeColor=Color.WhiteSmoke;
  directory.BorderStyle=BorderStyle.None;
  directory.Columns.Add("序号",46);
  directory.Columns.Add("原图 A",145);
  directory.Columns.Add("修改图 B",190);
  directory.RetrieveVirtualItem+=delegate(object sender,RetrieveVirtualItemEventArgs e){
   if(e.ItemIndex<0||e.ItemIndex>=Pairs.Count){e.Item=new ListViewItem("");return;}
   Pair pair=Pairs[e.ItemIndex];
   e.Item=new ListViewItem(new[]{(e.ItemIndex+1).ToString(),Path.GetFileName(pair.A),Path.GetFileName(pair.B)});
  };
  directory.SelectedIndexChanged+=delegate{
   if(syncingDirectory||closing||directory.SelectedIndices.Count==0)return;
   int target=directory.SelectedIndices[0];
   if(target>=0&&target<Pairs.Count&&target!=Index)Navigate(target-Index);
  };
  directoryTitle=new Label();
  directoryTitle.Dock=DockStyle.Top;
  directoryTitle.Height=48;
  directoryTitle.Padding=new Padding(8,6,0,0);
  directoryTitle.BackColor=Color.FromArgb(40,40,44);
  directoryTitle.ForeColor=Color.WhiteSmoke;
  directoryTitle.Text="图片目录 · 点击任意一组跳转";
  contentSplit.Panel1.Controls.Add(directory);
  contentSplit.Panel1.Controls.Add(directoryTitle);
  contentSplit.Panel2.Controls.Add(canvas);
 }

 void RebuildDirectory(){
  syncingDirectory=true;
  directory.BeginUpdate();
  try{
   directory.SelectedIndices.Clear();
   directory.VirtualListSize=Pairs.Count;
   directoryTitle.Text="图片目录 · "+Pairs.Count+" 组\n点击跳转 · 拖动右边界调整宽度";
   directory.Invalidate();
  }finally{directory.EndUpdate();syncingDirectory=false;}
  SyncDirectory();
 }

 void SyncDirectory(){
  if(directory==null||Pairs.Count==0||Index<0||Index>=directory.VirtualListSize)return;
  if(directory.SelectedIndices.Count==1&&directory.SelectedIndices[0]==Index)return;
  syncingDirectory=true;
  try{
   directory.SelectedIndices.Clear();
   directory.SelectedIndices.Add(Index);
   directory.EnsureVisible(Index);
  }finally{syncingDirectory=false;}
 }
}
