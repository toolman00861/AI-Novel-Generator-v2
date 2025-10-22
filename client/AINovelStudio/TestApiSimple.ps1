# 简化的智谱API测试脚本
Write-Host "=== Zhipu API Connection Test ===" -ForegroundColor Green

# 直接使用配置信息
$apiKey = "96a3c9215fe24d0d88a80c461378f9da.RJaYsNzDNCpDL9gU"
$baseUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions"
$model = "glm-4.6"

Write-Host "Provider Info:" -ForegroundColor Yellow
Write-Host "  API Key: Set" -ForegroundColor Green
Write-Host "  Base URL: $baseUrl" -ForegroundColor Green
Write-Host "  Model: $model" -ForegroundColor Green

# 准备API请求
$headers = @{
    "Authorization" = "Bearer $apiKey"
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

$body = @{
    model = $model
    messages = @(
        @{
            role = "user"
            content = "请说'Hello World'"
        }
    )
    temperature = 0.7
    max_tokens = 100
} | ConvertTo-Json -Depth 3

Write-Host "`nStarting API call test..." -ForegroundColor Yellow
Write-Host "Request URL: $baseUrl"
Write-Host "Request Body: $body"

try {
    $response = Invoke-RestMethod -Uri $baseUrl -Method Post -Headers $headers -Body $body -TimeoutSec 30
    
    Write-Host "`n✅ API connection test successful!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Yellow
    $response | ConvertTo-Json -Depth 5 | Write-Host
    
    if ($response.choices -and $response.choices[0].message.content) {
        Write-Host "`nGenerated content: $($response.choices[0].message.content)" -ForegroundColor Cyan
    }
}
catch {
    Write-Host "`n❌ API connection test failed!" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "HTTP Status Code: $statusCode" -ForegroundColor Red
        
        try {
            $errorContent = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorContent)
            $errorText = $reader.ReadToEnd()
            Write-Host "Error Response: $errorText" -ForegroundColor Red
        }
        catch {
            Write-Host "Cannot read error response content" -ForegroundColor Red
        }
    }
}

Write-Host "`nTest completed" -ForegroundColor Green