'use client';

import { useEffect, useState, useRef } from 'react';
import * as SignalR from '@microsoft/signalr';
import { Card, Col, Row, Progress, Statistic, Badge, Alert, notification } from 'antd';
import { ResponsiveContainer, AreaChart, Area, YAxis, XAxis } from 'recharts';
import { ThunderboltOutlined, AlertOutlined, DashboardOutlined, GlobalOutlined } from '@ant-design/icons';

interface SubstationMetric {
  id: string;
  substationCode: string;
  currentLoadMw: number;
  maxCapacityMw: number;
  percentageUtilisation: number;
  timestamp: string;
}

interface SubstationState extends SubstationMetric {
  history: Array<{ time: string; load: number }>;
}

export default function ControlRoomDashboard() {
  const [substations, setSubstations] = useState<Record<string, SubstationState>>({});
  const [isConnected, setIsConnected] = useState(false);
  const [isConnecting, setIsConnecting] = useState(true);
  const [api, contextHolder] = notification.useNotification();
  const firedAlerts = useRef<Set<string>>(new Set());

  useEffect(() => {
    async function fetchSnapshot() {
      try {
        const res = await fetch('http://localhost:5232/api/metrics/v1/snapshot');
        const data: SubstationMetric[] = await res.json();

        const initialState: Record<string, SubstationState> = {};
        data.forEach(metric => {
          initialState[metric.substationCode] = {
            ...metric,
            history: [{ time: new Date(metric.timestamp).toLocaleTimeString(), load: metric.currentLoadMw }]
          };
        });
        setSubstations(initialState);
      } catch (err) {
        console.error('Failed to retrieve grid snapshot tier:', err);
      }
    }

    fetchSnapshot();

    const connection = new SignalR.HubConnectionBuilder()
      .withUrl('http://localhost:5232/hub/metric', {
        skipNegotiation: true,
        transport: SignalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect()
      .build();

    connection.start()
      .then(() => setIsConnected(true))
      .catch(err => console.error('WebSocket connection failure:', err));

    connection.onreconnecting(() => {
      setIsConnected(false);
      setIsConnecting(true);
    });

    connection.onreconnected(() => {
      setIsConnected(true);
      setIsConnecting(false);
    });

    connection.onclose(() => {
      setIsConnected(false);
      setIsConnecting(false);
    });

    connection.on('ReceiveTelemetryUpdate', (metric: SubstationMetric) => {
      setSubstations(prev => {
        const existing = prev[metric.substationCode];
        const newTime = new Date(metric.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });

        const updatedHistory = existing
          ? [...existing.history, { time: newTime, load: metric.currentLoadMw }].slice(-12)
          : [{ time: newTime, load: metric.currentLoadMw }];

        const isOverloaded = metric.percentageUtilisation > 80;

        if (isOverloaded && !firedAlerts.current.has(metric.substationCode)) {
          firedAlerts.current.add(metric.substationCode);

          api.error({
            title: `ALERT: ${metric.substationCode}`,
            description: `Substation at critical capacity: ${Math.round(metric.percentageUtilisation)}% utilisation`,
            placement: 'topLeft',
            duration: 6,
            icon: <AlertOutlined style={{ color: '#ff4d4f' }} />,
            style: { borderLeft: '5px solid #ff4d4f', backgroundColor: '#fff1f0' }
          });
        } else if (!isOverloaded && firedAlerts.current.has(metric.substationCode)) {
          firedAlerts.current.delete(metric.substationCode);
        }

        return {
          ...prev,
          [metric.substationCode]: {
            ...metric,
            history: updatedHistory
          }
        };
      });
    });

    return () => {
      connection.stop();
    };
  }, [api]);

  const substationsList = Object.values(substations);
  const activeCount = substationsList.length;
  const criticalCount = substationsList.filter(s => s.percentageUtilisation > 80).length;
  const totalDemand = Math.round(substationsList.reduce((acc, curr) => acc + curr.currentLoadMw, 0));

  return (
    <div className="min-h-screen bg-slate-50 text-slate-800 font-sans antialiased">
      {contextHolder}

      <header className="bg-white border-b border-slate-200 px-6 py-4 flex justify-between items-center shadow-sm">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-1">
            <span className="text-[#005A36] text-xl font-bold lowercase">
              united
            </span>
            <span className="text-[#005A36] uppercase tracking-wider text-xs px-1 py-0.5 border border-[#005A36] rounded">
              grid
            </span>
          </div>
          <div className="h-6 w-px bg-slate-200" />
          <span className="text-xs font-bold text-slate-500 tracking-widest uppercase">Birmingham, UK - Control Centre</span>
        </div>
        {criticalCount > 0 && (
          <Alert
            title="ALERT: Utilisation exceeds maximum capacity on key substations. Action required."
            type="error"
            showIcon
          />
        )}
        <div>
          <Badge status={isConnected ? "success" : isConnecting ? "warning" : "error"} text={isConnected ? "Connected" : isConnecting ? "Connecting" : "Disconected"} />
        </div>
      </header>

      <div className="p-6 max-w-[1800px] mx-auto">

        <Row gutter={[24, 24]} className="mb-6">
          <Col xs={24} sm={8}>
            <Card variant="borderless" className="shadow-sm">
              <Statistic title="Active Substations" value={activeCount} prefix={<DashboardOutlined className="text-slate-400" />} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card variant='borderless' className="shadow-sm">
              <Statistic title="Total Demand" value={totalDemand} suffix="MW" prefix={<ThunderboltOutlined className="text-amber-500" />} />
            </Card>
          </Col>
          <Col xs={24} sm={8}>
            <Card variant="borderless" className={`shadow-sm transition-colors duration-300 ${criticalCount > 0 ? 'bg-red-50/50 border-l-4 border-red-500' : ''}`}>
              <Statistic title="Overload Status" value={criticalCount} suffix="Overloaded" prefix={<GlobalOutlined className={criticalCount > 0 ? 'text-red-500' : 'text-slate-400'} />} valueStyle={{ color: criticalCount > 0 ? '#cf1322' : 'inherit' }} />
            </Card>
          </Col>
        </Row>

        <div className="mb-4 mt-6 text-xs font-bold text-slate-400 uppercase tracking-wider">Monitored substations</div>

        <Row gutter={[16, 16]}>
          {substationsList.map((sub) => {
            const isOverloaded = sub.percentageUtilisation > 80;
            return (
              <Col xs={24} md={12} xl={8} xxl={6} key={sub.substationCode}>
                <Card
                  size="small"
                  variant='borderless'
                  className={`shadow-sm hover:shadow-md transition-all duration-200 ${isOverloaded ? 'border-red-300 bg-gradient-to-b from-white to-red-50/30' : 'border-slate-200'}`}
                  title={
                    <div className="flex justify-between items-center py-1">
                      <span className="font-bold text-slate-700">{sub.substationCode}</span>
                      <Badge count={isOverloaded ? "Critical" : "Stable"} style={{ backgroundColor: isOverloaded ? '#f5222d' : '#52c41a', fontSize: '10px' }} />
                    </div>
                  }
                >
                  <div className="px-1 pt-2">
                    <div className="flex justify-between items-baseline">
                      <span className="text-xs text-slate-400 font-medium">Utilisation</span>
                      <span className={`text-lg font-black tracking-tight ${isOverloaded ? 'text-red-500' : 'text-emerald-600'}`}>
                        {Math.round(sub.percentageUtilisation)}%
                      </span>
                    </div>

                    <Progress
                      percent={Math.round(sub.percentageUtilisation)}
                      showInfo={false}
                      status={isOverloaded ? "exception" : "normal"}
                      strokeColor={isOverloaded ? '#ff4d4f' : '#52c41a'}
                    />

                    <div className="h-20 w-full bg-slate-50 border border-slate-100 rounded p-1 mb-3 mt-2">
                      <ResponsiveContainer width="100%" height="100%">
                        <AreaChart data={sub.history} margin={{ top: 2, right: 2, left: 2, bottom: 2 }}>
                          <XAxis dataKey="time" hide />
                          <YAxis domain={[0, sub.maxCapacityMw]} hide />
                          <Area
                            type="monotone"
                            dataKey="load"
                            stroke={isOverloaded ? '#ff4d4f' : '#10b981'}
                            fill={isOverloaded ? '#ffeef0' : '#e6f7ff'}
                            strokeWidth={1.5}
                            isAnimationActive={false}
                          />
                        </AreaChart>
                      </ResponsiveContainer>
                    </div>

                    <div className="flex justify-between text-[11px] font-medium text-slate-500 border-t border-slate-100 pt-2">
                      <span>Current load: <b className="text-slate-700">{Math.round(sub.currentLoadMw)}MW</b></span>
                      <span>Maximum capacity: <b className="text-slate-700">{sub.maxCapacityMw}MW</b></span>
                    </div>
                  </div>
                </Card>
              </Col>
            );
          })}
        </Row>

      </div>
    </div>
  );
}