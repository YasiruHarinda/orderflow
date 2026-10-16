{{/*
Every template loops over .Values.services and passes this context to the helpers:
  root = the whole chart ($), name = service name (e.g. "orders-api"), svc = its values
*/}}

{{- define "orderflow.selectorLabels" -}}
app.kubernetes.io/name: {{ .name }}
app.kubernetes.io/instance: {{ .root.Release.Name }}
{{- end }}

{{- define "orderflow.labels" -}}
{{ include "orderflow.selectorLabels" . }}
app.kubernetes.io/part-of: orderflow
app.kubernetes.io/version: {{ .svc.image.tag | toString | quote }}
app.kubernetes.io/managed-by: {{ .root.Release.Service }}
helm.sh/chart: {{ printf "%s-%s" .root.Chart.Name .root.Chart.Version }}
{{- end }}

{{- define "orderflow.image" -}}
{{- $repository := .svc.image.repository -}}
{{- with .root.Values.global.imageRegistry }}{{ $repository = printf "%s/%s" . $repository }}{{ end -}}
{{ printf "%s:%s" $repository (.svc.image.tag | toString) }}
{{- end }}

{{/* Same hardened container security settings for app pods and migration jobs. */}}
{{- define "orderflow.containerSecurity" -}}
allowPrivilegeEscalation: false
readOnlyRootFilesystem: true
capabilities:
  drop: ["ALL"]
{{- end }}

{{- define "orderflow.podSecurity" -}}
runAsNonRoot: true
runAsUser: 1654 # the "app" user in .NET images
runAsGroup: 1654
fsGroup: 1654
seccompProfile:
  type: RuntimeDefault
{{- end }}

{{- define "orderflow.env" -}}
- name: ASPNETCORE_ENVIRONMENT
  value: {{ .root.Values.global.environment | quote }}
- name: OTEL_SERVICE_NAME
  value: {{ .name | quote }}
- name: OTEL_RESOURCE_ATTRIBUTES
  value: {{ printf "service.version=%s,deployment.environment.name=%s" (.svc.image.tag | toString) .root.Values.global.environment | quote }}
{{- with .root.Values.global.otlpEndpoint }}
- name: OTEL_EXPORTER_OTLP_ENDPOINT
  value: {{ . | quote }}
{{- end }}
{{- range $key, $value := .svc.env }}
- name: {{ $key }}
  value: {{ $value | quote }}
{{- end }}
{{- end }}

{{/* The pod template, shared by Deployment (normal) and Rollout (canary). */}}
{{- define "orderflow.podTemplate" -}}
metadata:
  labels:
    {{- include "orderflow.selectorLabels" . | nindent 4 }}
    app.kubernetes.io/part-of: orderflow
    app.kubernetes.io/version: {{ .svc.image.tag | toString | quote }}
spec:
  serviceAccountName: {{ .name }}
  automountServiceAccountToken: false # the apps never call the Kubernetes API
  securityContext:
    {{- include "orderflow.podSecurity" . | nindent 4 }}
  # Spread replicas over different nodes, so one node failure doesn't take the service down.
  topologySpreadConstraints:
    - maxSkew: 1
      topologyKey: kubernetes.io/hostname
      whenUnsatisfiable: ScheduleAnyway
      labelSelector:
        matchLabels:
          {{- include "orderflow.selectorLabels" . | nindent 10 }}
  terminationGracePeriodSeconds: 30
  containers:
    - name: {{ .name }}
      image: {{ include "orderflow.image" . }}
      imagePullPolicy: {{ .root.Values.global.imagePullPolicy }}
      ports:
        - name: http
          containerPort: 8080
      env:
        {{- include "orderflow.env" . | nindent 8 }}
      {{- with .svc.secretName }}
      envFrom:
        - secretRef:
            name: {{ . }}
      {{- end }}
      # startup:   give the app up to 60s to start before the other probes begin
      # liveness:  "is the process stuck?"  -> Kubernetes restarts the container
      # readiness: "can it serve traffic?"  -> Kubernetes removes it from the Service
      startupProbe:
        httpGet: { path: /health/live, port: http }
        periodSeconds: 2
        failureThreshold: 30
      livenessProbe:
        httpGet: { path: /health/live, port: http }
        periodSeconds: 10
        failureThreshold: 3
      readinessProbe:
        httpGet: { path: /health/ready, port: http }
        periodSeconds: 5
        failureThreshold: 3
      lifecycle:
        # On shutdown, wait 5s before stopping so the Service stops sending new requests
        # first. Zero failed requests during rolling updates.
        preStop:
          sleep:
            seconds: 5
      resources:
        {{- toYaml .svc.resources | nindent 8 }}
      securityContext:
        {{- include "orderflow.containerSecurity" . | nindent 8 }}
      volumeMounts:
        - name: tmp
          mountPath: /tmp # the root filesystem is read-only; .NET needs a writable /tmp
  volumes:
    - name: tmp
      emptyDir: {}
{{- end }}
