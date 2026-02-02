# 1. Dùng image SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# --- SỬA ĐOẠN NÀY QUAN TRỌNG ---
# Copy file csproj từ đúng đường dẫn thư mục con
# Cấu trúc: COPY ["Đường_dẫn_trên_GitHub", "Đường_dẫn_trong_Docker"]
COPY ["asp_project/asp_project/asp_project.csproj", "asp_project/asp_project/"]

# Restore các thư viện dựa trên file vừa copy
RUN dotnet restore "asp_project/asp_project/asp_project.csproj"
# --------------------------------

# Copy toàn bộ mã nguồn còn lại
COPY . .

# Chuyển thư mục làm việc vào trong nơi chứa code chính
WORKDIR "/src/asp_project/asp_project"

# Build và Publish ra thư mục /app/out
RUN dotnet publish -c Release -o /app/out

# 2. Dùng image Runtime để chạy (nhẹ hơn)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Mở port (Render thường tự map port 8080)
EXPOSE 8080

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "asp_project.dll"]