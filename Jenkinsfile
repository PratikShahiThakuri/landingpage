pipeline {
    agent { label 'docker-agent' }

    environment {
        TARGET_ENV = 'prod'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Deploy (ci/run.sh)') {
            steps {
                echo 'Pulling secure JsonConfig and running deployment...'
                // Inject the managed JSON file into the workspace
                configFileProvider([configFile(fileId: 'landinggooglejson', targetLocation: 'google-credentials.json')]) {
                    withCredentials([string(credentialsId: 'GOOGLE_SHEET_ID', variable: 'SPREADSHEET_ID')]) {
                        sh 'chmod +x ci/run.sh'
                        sh './ci/run.sh'
                    }
                }
            }
        }
    }

    post {
        always {
            echo 'Cleaning up workspace...'
            cleanWs()
        }
        success {
            echo 'Build and Deployment Successful!'
        }
        failure {
            echo 'Pipeline failed. Check the logs for details.'
        }
    }
}