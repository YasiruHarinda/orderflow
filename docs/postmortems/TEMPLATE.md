# Postmortem: <short title>

> Blameless: we describe what the *system* allowed to happen, not who made a mistake.

| | |
|---|---|
| **Date** | YYYY-MM-DD |
| **Duration** | hh:mm (from first impact to full recovery) |
| **Severity** | SEV1 (customers affected, no workaround) / SEV2 / SEV3 / Exercise |
| **Author** | |
| **Status** | Draft / Reviewed / Action items done |

## Summary
Two or three sentences: what happened, who was affected, how it was fixed.

## Impact
- Requests failed: … (from Grafana: error rate × request rate × duration)
- Orders affected: … (`SELECT "Status", COUNT(*) FROM orders.orders WHERE "CreatedAt" BETWEEN … GROUP BY 1`)
- Error budget used: … % of the 30-day budget

## Timeline (UTC)
| Time | Event |
|---|---|
| hh:mm | First alert: … |
| hh:mm | … |
| hh:mm | Recovered |

## Root cause
The technical chain of events. Ask "why?" until you reach something the system should have prevented.

## What went well
- …

## What went badly
- …

## Action items
| Action | Type (prevent / detect / mitigate) | Owner | Issue |
|---|---|---|---|
| … | | | |

## Evidence
Links to dashboards (with the time range), traces, logs, k6 output, commits.
