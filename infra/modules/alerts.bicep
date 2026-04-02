param namePrefix string
param appServicePlanResourceId string
param sqlDatabaseResourceId string
param notificationEmailReceivers array = []
param actionGroupShortName string = 'fleethub'

resource actionGroup 'Microsoft.Insights/actionGroups@2022-06-01' = if (length(notificationEmailReceivers) > 0) {
  name: '${namePrefix}-ag'
  location: 'global'
  properties: {
    enabled: true
    groupShortName: actionGroupShortName
    emailReceivers: [for receiver in notificationEmailReceivers: {
      name: receiver.name
      emailAddress: receiver.emailAddress
      useCommonAlertSchema: true
    }]
  }
}

resource appServiceCpuAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${namePrefix}-appservice-cpu'
  location: 'global'
  properties: {
    description: 'Alert when App Service plan CPU is high.'
    severity: 2
    enabled: true
    scopes: [
      appServicePlanResourceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighCpu'
          metricName: 'CpuPercentage'
          operator: 'GreaterThan'
          threshold: 80
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: length(notificationEmailReceivers) > 0 ? [
      {
        actionGroupId: actionGroup.id
      }
    ] : []
  }
}

resource sqlCpuAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${namePrefix}-sql-cpu'
  location: 'global'
  properties: {
    description: 'Alert when SQL database CPU is high.'
    severity: 2
    enabled: true
    scopes: [
      sqlDatabaseResourceId
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'HighSqlCpu'
          metricName: 'cpu_percent'
          operator: 'GreaterThan'
          threshold: 80
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: length(notificationEmailReceivers) > 0 ? [
      {
        actionGroupId: actionGroup.id
      }
    ] : []
  }
}

output actionGroupId string = length(notificationEmailReceivers) > 0 ? actionGroup.id : ''
