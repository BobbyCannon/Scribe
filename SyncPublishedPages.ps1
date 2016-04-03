#param (
    #[Parameter(Mandatory=$true)]
    #[string] $SourceServer,
    #[Parameter(Mandatory=$true)]
    #[string] $DestinationServer
#)

#$SourceServer
#$DestinationServer

$sourcePages = Get-ScribePages -ServerName rand-wiki -PerPage 1000000
$publishedPages = $sourcePages.Results | Where { $_.ApprovalStatus -eq 'approved' -and $_.IsPublished }
#$publishedPages | ft
$destinationPages = Get-ScribePages -ServerName $DestinationServer

foreach ($page in $publishedPages)
{
	$existingPage = $destinationPages.Results | Where { $_.Id -eq $page.Id } | Select -First 1
	if ($existingPage -ne $null) {
		Write-Host $existingPage.Id "already exists"
	} else {
		Write-Host $existingPage.Id "already exists"
	}
	
	#$destinationPages | ft
}