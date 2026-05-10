# PowerShell script to push test messages to RabbitMQ 'document-files-queue'
# Uses RabbitMQ Management HTTP API (port 15672)

$RabbitMQHost = "localhost"
$RabbitMQPort = 15672
$UserName = "rabbitmq"
$Password = "rabbitmq"
$QueueName = "document-files-queue"

$BaseUrl = "http://${RabbitMQHost}:${RabbitMQPort}"
$AuthHeader = "Basic " + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${UserName}:${Password}"))
$Headers = @{ Authorization = $AuthHeader; "Content-Type" = "application/json" }

# Ensure the queue exists (declaring a durable queue)
$QueueBody = @{
    durable = $true
    auto_delete = $false
} | ConvertTo-Json

try {
    Invoke-RestMethod -Uri "${BaseUrl}/api/queues/%2f/${QueueName}" `
        -Method Put `
        -Headers $Headers `
        -Body $QueueBody | Out-Null
    Write-Host "Queue '${QueueName}' is ready." -ForegroundColor Green
}
catch {
    Write-Warning "Could not declare queue (may already exist): $_"
}

# Messages to publish (NewDocumentDto format)
$messages = @(
    @{
        Id         = [Guid]::NewGuid().ToString()
        FileName   = "report-2026-q1.pdf"
        FileType   = 1  # Pdf
        FileSize   = 102400
        CreatedAt  = (Get-Date).ToUniversalTime().ToString("o")
    },
    @{
        Id         = [Guid]::NewGuid().ToString()
        FileName   = "invoice-42.pdf"
        FileType   = 1  # Pdf
        FileSize   = 51200
        CreatedAt  = (Get-Date).ToUniversalTime().ToString("o")
    },
    @{
        Id         = [Guid]::NewGuid().ToString()
        FileName   = "contract-2026.pdf"
        FileType   = 1  # Pdf
        FileSize   = 256000
        CreatedAt  = (Get-Date).ToUniversalTime().ToString("o")
    }
)

for ($i = 0; $i -lt $messages.Count; $i++) {
    $payload = $messages[$i] | ConvertTo-Json -Compress

    $body = @{
        properties = @{}
        routing_key = $QueueName
        payload = $payload
        payload_encoding = "string"
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "${BaseUrl}/api/exchanges/%2f/amq.default/publish" `
            -Method Post `
            -Headers $Headers `
            -Body $body

        if ($response.routed -eq $true) {
            Write-Host "[$($i+1)/3] Published: $($messages[$i].FileName) (Id: $($messages[$i].Id))" -ForegroundColor Green
        }
        else {
            Write-Host "[$($i+1)/3] Message not routed (no consumers bound). Published anyway: $($messages[$i].FileName)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "[$($i+1)/3] Failed to publish '$($messages[$i].FileName)': $_" -ForegroundColor Red
    }
}

Write-Host "Done." -ForegroundColor Cyan