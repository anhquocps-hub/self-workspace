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

**Open Workspace** và **Open Project** mở toàn bộ folder trước, rồi mở các link
và file media theo thứ tự đang lưu. Folder mở bằng File Explorer, URL mở bằng
trình duyệt mặc định, file media mở bằng ứng dụng mặc định của Windows.
Mục trống được bỏ qua, mục trùng chỉ mở một lần. Nếu một mục không mở được,
ứng dụng tiếp tục mở các mục còn lại và hiển thị thông báo lỗi tổng hợp.
Nút Open cạnh mỗi link vẫn mở riêng link đó.

Nếu có hơn **5 link**, hơn **3 folder** hoặc hơn **2 file media**, ứng dụng hỏi
xác nhận trước khi mở bất kỳ mục nào, kèm số lượng từng loại. Chọn **Yes** để
mở tất cả hoặc **No** (mặc định) để hủy. Đúng ngưỡng vẫn mở ngay. Số lượng
không tính mục trống/trùng, nhưng vẫn tính đường dẫn chưa tồn tại.
Không giới hạn số tài nguyên được lưu trong dự án.


## Card tóm tắt và chi tiết dự án

Card hiển thị tên, trạng thái, ghi chú ngắn, deadline và số lượng tài nguyên.
Nhấn vùng thông tin hoặc nút **Details** để mở cửa sổ chi tiết có thể thay đổi
kích thước. Nút **Open Project** trên card mở toàn bộ folder, link và file media.

Cửa sổ chi tiết hiển thị toàn bộ ghi chú, liên kết/media và folder. Tại đây
có thể đổi trạng thái, thêm folder, chọn folder chính, sửa hoặc xóa dự án.
Các thay đổi được lưu ngay; **Close** không hoàn tác. Hộp thoại **Edit** vẫn
cho phép Cancel để bỏ các chỉnh sửa chưa lưu. Xóa dự án cần xác nhận.
