# Workspace Hub

Ứng dụng desktop WPF quản lý dự án: tạo/sửa/xóa, trạng thái, deadline,
thư mục làm việc và đường dẫn media. Hỗ trợ lọc trạng thái và sắp xếp deadline.

## Yêu cầu và chạy

Windows với .NET 10 SDK (Visual Studio hỗ trợ .NET 10 nếu dùng IDE).

```powershell
dotnet build workspace-hub.csproj -c Debug
dotnet build workspace-hub.csproj -c Release
dotnet run --project workspace-hub.csproj
```

## Cấu trúc

- `Views/`: cửa sổ chính, hộp thoại tạo/sửa và code-behind.
- `ViewModels/`: dữ liệu hiển thị, bộ lọc và command của màn hình.
- `Models/`: dữ liệu dự án.
- `Services/`: đọc/ghi dữ liệu JSON.
- `Commands/`: triển khai ICommand dùng chung.
- `Controls/`: ProjectCard và tương tác trên thẻ dự án.
- `Converters/`: chuyển đổi giá trị cho binding.
- `Resources/`: style dùng chung.
- `App.xaml`: điểm khởi động và khai báo resource toàn ứng dụng.

Namespace theo thư mục, bắt đầu bằng `workspace_hub`.
Các tệp XAML và code-behind được đặt cạnh nhau.
Dự án giữ một assembly; một phần logic giao diện vẫn nằm trong code-behind.
MainWindow sử dụng collection và collection view của MainViewModel.

## Dữ liệu

Dữ liệu lưu tại `%LocalAppData%/WorkspaceHub/projects.json`.
Sao lưu thư mục dữ liệu trước khi kiểm tra các thao tác tạo/sửa/xóa bằng dữ liệu thử.
Xóa dự án trong ứng dụng không xóa thư mục hoặc tệp của dự án trên ổ đĩa.


## Thêm liên kết

Trong cửa sổ tạo hoặc sửa dự án, nhập URL vào mục **Liên kết**, rồi nhấn
**Thêm** hoặc Enter. Mỗi liên kết có nút **Xóa**. URL phải bắt đầu bằng
http:// hoặc https://; có thể dùng link YouTube, GitHub hoặc website dự án.
Nếu ô URL còn nội dung khi lưu, ứng dụng sẽ kiểm tra và thêm link đó.

Dự án cần tên và ít nhất một folder hoặc URL hợp lệ. Khi tạo dự án chỉ có
URL, để folder trống; nút Clear bỏ folder đã chọn. Khi sửa, Cancel giữ
nguyên dữ liệu. Các tệp media cũ vẫn được bảo toàn.

Open Workspace mở folder chính nếu có; nếu không có folder, mở URL đầu
tiên bằng trình duyệt mặc định. Nút Open cạnh mỗi link mở riêng link đó.
