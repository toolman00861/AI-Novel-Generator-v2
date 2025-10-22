# 智谱API连接测试脚本
Write-Host "=== 智谱API连接测试 ===" -ForegroundColor Green

# 读取配置文件
$configPath = "bin\Debug\net8.0-windows\appsettings.client.json"
if (-not (Test-Path $configPath)) {
    Write-Host "配置文件不存在: $configPath" -ForegroundColor Red
    exit 1
}

$config = Get-Content $configPath | ConvertFrom-Json

# 查找智谱供应商
$zhipuProvider = $config.Providers | Where-Object { $_.Name -eq "\u667A\u8C31" }
if (-not $zhipuProvider) {
    Write-Host "未找到智谱供应商配置" -ForegroundColor Red
    exit 1
}

Write-Host "供应商信息:" -ForegroundColor Yellow
Write-Host "  名称: $($zhipuProvider.Name)"
Write-Host "  类型: $($zhipuProvider.Vendor)"
Write-Host "  基础URL: $($zhipuProvider.BaseUrl)"
Write-Host "  API密钥: $(if ($zhipuProvider.ApiKey) { 'Set' } else { 'NotSet' })"
Write-Host "  默认模型: $($zhipuProvider.DefaultModel)"

if (-not $zhipuProvider.ApiKey) {
    Write-Host "API key not set, cannot test" -ForegroundColor Red
    exit 1
}

# 准备API请求
$headers = @{
    "Authorization" = "Bearer $($zhipuProvider.ApiKey)"
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

$body = @{
    model = $zhipuProvider.DefaultModel
    messages = @(
        @{
            role = "user"
            content = "请说'Hello World'"
        }
    )
    temperature = 0.7
    max_tokens = 100
} | ConvertTo-Json -Depth 3

Write-Host "`n开始测试API调用..." -ForegroundColor Yellow
Write-Host "请求URL: $($zhipuProvider.BaseUrl)"
Write-Host "请求体: $body"

try {
    $response = Invoke-RestMethod -Uri $zhipuProvider.BaseUrl -Method Post -Headers $headers -Body $body -TimeoutSec 30
    
    Write-Host "`n✅ API连接测试成功!" -ForegroundColor Green
    Write-Host "响应内容:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5 | Write-Host
    
    if ($response.choices -and $response.choices[0].message.content) {
        Write-Host "`n生成的内容: $($response.choices[0].message.content)" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "`n❌ API连接测试失败!" -ForegroundColor Red
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "HTTP状态码: $statusCode" -ForegroundColor Red
        
        try {
            $errorContent = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorContent)
            $errorText = $reader.ReadToEnd()
            Write-Host "错误响应: $errorText" -ForegroundColor Red
        }
        catch {
            Write-Host "无法读取错误响应内容" -ForegroundColor Red
        }
    }
}

Write-Host "`n测试完成" -ForegroundColor Green