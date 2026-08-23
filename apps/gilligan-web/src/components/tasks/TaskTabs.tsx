"use client";

export type TaskTab = "today" | "week" | "custom";

type TaskTabsProps = {
  activeTab: TaskTab;
  onSelectTab: (tab: TaskTab) => void;
};

const tabs: Array<{ label: string; value: TaskTab }> = [
  { label: "Today", value: "today" },
  { label: "This Week", value: "week" },
  { label: "Custom", value: "custom" },
];

export function TaskTabs({ activeTab, onSelectTab }: TaskTabsProps) {
  return (
    <div className="tab-list task-tabs" role="tablist" aria-label="Task date ranges">
      {tabs.map((tab) => (
        <button
          key={tab.value}
          className={activeTab === tab.value ? "tab-button tab-button-active" : "tab-button"}
          type="button"
          role="tab"
          aria-selected={activeTab === tab.value}
          onClick={() => onSelectTab(tab.value)}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}
