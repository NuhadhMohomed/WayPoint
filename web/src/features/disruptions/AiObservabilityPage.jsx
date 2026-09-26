import React, { useState, useEffect, useCallback } from 'react'
import { disruptionApi } from './disruptionApi'
import { useDisruptionStore } from '@/store/disruptionStore'
import { Card, CardHeader } from '@/components/ui/Card'
import { Button } from '@/components/ui/Button'
import { TransitBadge } from '@/components/ui/TransitBadge'
import {
  Bot, Cpu, Wrench, ShieldCheck, CheckCircle2, XCircle,
  AlertTriangle, Clock, ArrowRight, ChevronDown, ChevronRight,
  Code2, Loader2, RefreshCw, Layers, ShieldAlert, Sparkles
} from 'lucide-react'

export function AiObservabilityPage() {
  const {
    aiWorkflows,
    workflowsLoading,
    setAiWorkflows,
    setWorkflowsLoading,
    activeWorkflow,
    setActiveWorkflow,
    activeWorkflowLoading,
    setActiveWorkflowLoading,
  } = useDisruptionStore()

  const [expandedTools, setExpandedTools] = useState({})
  const [selectedWorkflowId, setSelectedWorkflowId] = useState(null)
  const [errorMessage, setErrorMessage] = useState(null)

  const toggleToolExpanded = (toolId) => {
    setExpandedTools((prev) => ({
      ...prev,
      [toolId]: !prev[toolId],
    }))
  }

  const loadWorkflows = useCallback(async () => {
    setWorkflowsLoading(true)
    setErrorMessage(null)
    try {
      const data = await disruptionApi.getAiWorkflows({ pageNumber: 1, pageSize: 20 })
      setAiWorkflows(data)

      const items = data.items || data
      if (items.length > 0 && !selectedWorkflowId) {
        setSelectedWorkflowId(items[0].id)
      }
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || 'Failed to retrieve AI workflow observability traces.')
    } finally {
      setWorkflowsLoading(false)
    }
  }, [setAiWorkflows, setWorkflowsLoading, selectedWorkflowId])

  const loadWorkflowDetail = useCallback(async (workflowId) => {
    if (!workflowId) return
    setActiveWorkflowLoading(true)
    try {
      const detail = await disruptionApi.getAiWorkflowById(workflowId)
      setActiveWorkflow(detail)
    } catch (err) {
      setErrorMessage(err.response?.data?.detail || 'Failed to load workflow execution trace.')
    } finally {
      setActiveWorkflowLoading(false)
    }
  }, [setActiveWorkflow, setActiveWorkflowLoading])

  useEffect(() => {
    loadWorkflows()
  }, [loadWorkflows])

  useEffect(() => {
    if (selectedWorkflowId) {
      loadWorkflowDetail(selectedWorkflowId)
    }
  }, [selectedWorkflowId, loadWorkflowDetail])

  const formatJson = (jsonString) => {
    try {
      const parsed = typeof jsonString === 'string' ? JSON.parse(jsonString) : jsonString
      return JSON.stringify(parsed, null, 2)
    } catch {
      return jsonString || '{}'
    }
  }

  return (
    <div className="space-y-6">
      {/* Error Banner */}
      {errorMessage && (
        <div className="p-4 bg-rose-950/60 border border-rose-800 text-rose-300 rounded-xl text-sm flex items-center gap-2">
          <XCircle className="w-5 h-5 flex-shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Governance & SafeFailure Header Banner */}
      <div className="p-5 bg-slate-900 border border-slate-800 rounded-2xl flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="w-11 h-11 rounded-xl bg-purple-500/10 border border-purple-500/20 flex items-center justify-center text-purple-400 flex-shrink-0">
            <Sparkles className="w-6 h-6 animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-base font-bold text-white">AI Multi-Agent Observability & Traces (WEB-10)</h3>
              <span className="px-2 py-0.5 text-[10px] font-semibold rounded bg-emerald-500/20 text-emerald-300 border border-emerald-500/30">
                SafeFailure Guardrails Active
              </span>
            </div>
            <p className="text-xs text-slate-400 mt-1">
              End-to-end execution tracking, tool call sandboxing, deterministic validation gates, and immutable audit persistence.
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 self-start md:self-auto">
          <Button variant="outline" size="sm" onClick={loadWorkflows} disabled={workflowsLoading}>
            <RefreshCw className={`w-4 h-4 mr-1.5 ${workflowsLoading ? 'animate-spin' : ''}`} />
            Refresh Traces
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Workflows Queue */}
        <div className="lg:col-span-4 space-y-4">
          <Card>
            <CardHeader
              title="Workflow Traces"
              subtitle="Select an execution session to inspect multi-agent steps."
            />

            {workflowsLoading && !activeWorkflow ? (
              <div className="text-center py-12 text-slate-400">
                <Loader2 className="w-6 h-6 animate-spin mx-auto mb-2 text-waypoint-blue" />
                Loading workflow traces...
              </div>
            ) : aiWorkflows.length === 0 ? (
              <div className="text-center py-12 text-slate-400 text-xs">
                No AI workflow traces recorded yet. Seed data provides sample traces.
              </div>
            ) : (
              <div className="space-y-2.5">
                {aiWorkflows.map((wf) => (
                  <div
                    key={wf.id}
                    onClick={() => setSelectedWorkflowId(wf.id)}
                    className={`p-3.5 rounded-xl border cursor-pointer transition-all ${
                      selectedWorkflowId === wf.id
                        ? 'bg-waypoint-blue/15 border-waypoint-blue/60 shadow-md ring-1 ring-waypoint-blue/40'
                        : 'bg-slate-950 border-slate-800/80 hover:border-slate-700'
                    }`}
                  >
                    <div className="flex items-center justify-between mb-1.5">
                      <span className="font-mono text-[10px] text-sky-400">WF-{wf.id.substring(0, 8)}</span>
                      <span className={`px-2 py-0.5 text-[10px] font-semibold rounded-full border ${
                        wf.status === 'Completed'
                          ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30'
                          : wf.status === 'Failed'
                          ? 'bg-rose-500/20 text-rose-300 border-rose-500/30'
                          : 'bg-amber-500/20 text-amber-300 border-amber-500/30'
                      }`}>
                        {wf.status}
                      </span>
                    </div>

                    <h4 className="text-xs font-semibold text-white line-clamp-2 leading-relaxed">
                      {wf.objective}
                    </h4>

                    <div className="flex items-center justify-between text-[10px] text-slate-400 mt-2 pt-2 border-t border-slate-800/60 font-mono">
                      <span>{new Date(wf.startedAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                      <span>{wf.steps?.length || 3} steps</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>

          {/* AI Governance Policy Card */}
          <div className="p-4 bg-slate-950 border border-slate-800 rounded-2xl space-y-2.5 text-xs">
            <div className="flex items-center gap-2 text-white font-semibold">
              <ShieldAlert className="w-4 h-4 text-amber-400" />
              <span>WayPoint AI Safety Directives</span>
            </div>
            <ul className="text-slate-400 space-y-1.5 list-disc pl-4 text-[11px] leading-relaxed">
              <li>AI agents cannot directly confirm passenger payment.</li>
              <li>AI agents cannot directly modify operational timetable records.</li>
              <li>High-impact dispatch requires deterministic Transport Manager approval (<strong>BR-APPROVAL-001</strong>).</li>
              <li>Deterministic validation rules always override LLM output schema recommendations.</li>
            </ul>
          </div>
        </div>

        {/* Right Column: Execution Timeline, Tool Logs & Deterministic Validations */}
        <div className="lg:col-span-8 space-y-5">
          {activeWorkflowLoading ? (
            <Card>
              <div className="text-center py-16 text-slate-400">
                <Loader2 className="w-8 h-8 animate-spin mx-auto mb-2 text-waypoint-blue" />
                Loading multi-agent execution timeline...
              </div>
            </Card>
          ) : !activeWorkflow ? (
            <Card>
              <div className="text-center py-16 text-slate-400">
                Select a workflow trace from the left queue to view execution steps.
              </div>
            </Card>
          ) : (
            <div className="space-y-5">
              {/* Workflow Overview Card */}
              <Card>
                <div className="space-y-3">
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-800 pb-3">
                    <div>
                      <span className="text-[10px] font-mono text-sky-400">Trace Session ID: {activeWorkflow.id}</span>
                      <h3 className="text-base font-bold text-white mt-0.5">{activeWorkflow.objective}</h3>
                    </div>
                    <span className={`px-2.5 py-1 text-xs font-semibold rounded-full border self-start sm:self-auto ${
                      activeWorkflow.status === 'Completed'
                        ? 'bg-emerald-500/20 text-emerald-300 border-emerald-500/30'
                        : 'bg-rose-500/20 text-rose-300 border-rose-500/30'
                    }`}>
                      {activeWorkflow.status}
                    </span>
                  </div>

                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 text-xs">
                    <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800">
                      <span className="text-slate-400 block text-[10px]">Execution Started</span>
                      <span className="font-mono text-white font-medium">
                        {new Date(activeWorkflow.startedAt).toLocaleTimeString()}
                      </span>
                    </div>
                    <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800">
                      <span className="text-slate-400 block text-[10px]">Execution Completed</span>
                      <span className="font-mono text-white font-medium">
                        {activeWorkflow.completedAt ? new Date(activeWorkflow.completedAt).toLocaleTimeString() : 'In Progress'}
                      </span>
                    </div>
                    <div className="bg-slate-950 p-2.5 rounded-xl border border-slate-800 col-span-2 sm:col-span-1">
                      <span className="text-slate-400 block text-[10px]">Multi-Agent Count</span>
                      <span className="font-medium text-purple-300">
                        {activeWorkflow.steps?.length || 0} Specialised Agents
                      </span>
                    </div>
                  </div>
                </div>
              </Card>

              {/* Execution Steps Timeline */}
              <div className="space-y-4">
                <h4 className="text-sm font-bold uppercase tracking-wider text-slate-400 flex items-center gap-2">
                  <Layers className="w-4 h-4 text-waypoint-blue" />
                  Execution Timeline & Tool Sandboxing
                </h4>

                {(!activeWorkflow.steps || activeWorkflow.steps.length === 0) ? (
                  <Card>
                    <p className="text-xs text-slate-400 text-center py-6">No steps recorded for this workflow session.</p>
                  </Card>
                ) : (
                  activeWorkflow.steps.map((step, idx) => (
                    <div
                      key={step.id || idx}
                      className="bg-slate-900 border border-slate-800 rounded-2xl p-5 space-y-4 shadow-sm hover:border-slate-700 transition-all"
                    >
                      {/* Step Header */}
                      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-slate-800 pb-3">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-lg bg-waypoint-blue/20 border border-waypoint-blue/40 flex items-center justify-center text-waypoint-blue font-bold text-xs">
                            #{step.stepOrder || idx + 1}
                          </div>
                          <div>
                            <div className="flex items-center gap-2">
                              <h5 className="text-sm font-bold text-white">{step.agentName}</h5>
                              <span className="px-2 py-0.5 text-[10px] font-mono rounded bg-slate-800 text-slate-300">
                                Autonomous Step
                              </span>
                            </div>
                            <p className="text-xs text-slate-300 mt-0.5">{step.stepDescription}</p>
                          </div>
                        </div>

                        <span className="text-[11px] font-mono text-slate-400 whitespace-nowrap self-start sm:self-auto">
                          {new Date(step.executedAt).toLocaleTimeString()}
                        </span>
                      </div>

                      {/* Tool Calls inside Step */}
                      {step.toolCalls && step.toolCalls.length > 0 && (
                        <div className="space-y-2">
                          <span className="text-[11px] font-semibold uppercase text-slate-400 flex items-center gap-1.5">
                            <Wrench className="w-3.5 h-3.5 text-amber-400" />
                            Tool Calls ({step.toolCalls.length})
                          </span>

                          <div className="space-y-2">
                            {step.toolCalls.map((tc) => {
                              const isExp = expandedTools[tc.id]
                              return (
                                <div
                                  key={tc.id}
                                  className="bg-slate-950 border border-slate-800 rounded-xl overflow-hidden transition-all"
                                >
                                  <div
                                    onClick={() => toggleToolExpanded(tc.id)}
                                    className="p-3 flex items-center justify-between cursor-pointer hover:bg-slate-800/40 text-xs"
                                  >
                                    <div className="flex items-center gap-2">
                                      <Code2 className="w-4 h-4 text-sky-400" />
                                      <span className="font-mono font-bold text-white">{tc.toolName}</span>
                                    </div>
                                    <div className="flex items-center gap-3">
                                      <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-slate-900 text-slate-300 border border-slate-800">
                                        {tc.durationMs}ms
                                      </span>
                                      {isExp ? (
                                        <ChevronDown className="w-4 h-4 text-slate-400" />
                                      ) : (
                                        <ChevronRight className="w-4 h-4 text-slate-400" />
                                      )}
                                    </div>
                                  </div>

                                  {isExp && (
                                    <div className="p-3 border-t border-slate-800 bg-slate-950/80 space-y-2.5 text-xs font-mono">
                                      <div>
                                        <span className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider block mb-1">
                                          Arguments JSON
                                        </span>
                                        <pre className="bg-slate-900 border border-slate-800 p-2.5 rounded-lg text-emerald-300 text-[11px] overflow-x-auto whitespace-pre-wrap">
                                          {formatJson(tc.argumentsJson)}
                                        </pre>
                                      </div>

                                      <div>
                                        <span className="text-[10px] font-semibold text-slate-400 uppercase tracking-wider block mb-1">
                                          Result JSON
                                        </span>
                                        <pre className="bg-slate-900 border border-slate-800 p-2.5 rounded-lg text-sky-300 text-[11px] overflow-x-auto whitespace-pre-wrap">
                                          {formatJson(tc.resultJson)}
                                        </pre>
                                      </div>
                                    </div>
                                  )}
                                </div>
                              )
                            })}
                          </div>
                        </div>
                      )}

                      {/* Deterministic Validation Results */}
                      {step.validationResults && step.validationResults.length > 0 && (
                        <div className="space-y-2 pt-1 border-t border-slate-800/60">
                          <span className="text-[11px] font-semibold uppercase text-slate-400 flex items-center gap-1.5">
                            <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
                            Deterministic Backend Rules Validation
                          </span>

                          <div className="space-y-2">
                            {step.validationResults.map((vr, vIdx) => (
                              <div
                                key={vr.id || vIdx}
                                className={`p-3 rounded-xl border text-xs flex items-start gap-2.5 ${
                                  vr.passed
                                    ? 'bg-emerald-950/20 border-emerald-900/40 text-emerald-200'
                                    : 'bg-rose-950/20 border-rose-900/40 text-rose-200'
                                }`}
                              >
                                {vr.passed ? (
                                  <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0 mt-0.5" />
                                ) : (
                                  <XCircle className="w-4 h-4 text-rose-400 flex-shrink-0 mt-0.5" />
                                )}
                                <div className="space-y-0.5">
                                  <div className="font-semibold text-white">{vr.ruleName}</div>
                                  <div className="text-[11px] text-slate-300">{vr.validationDetails}</div>
                                </div>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  ))
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
