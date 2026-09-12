import { Route, Routes } from 'react-router-dom';
import Layout from './components/Layout.jsx';
import ProtectedRoute from './components/ProtectedRoute.jsx';
import LoginPage from './pages/LoginPage.jsx';
import DashboardPage from './pages/DashboardPage.jsx';
import BlotterPage from './pages/BlotterPage.jsx';
import NewOrderPage from './pages/NewOrderPage.jsx';
import TradeDetailPage from './pages/TradeDetailPage.jsx';
import NotFoundPage from './pages/NotFoundPage.jsx';

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<DashboardPage />} />
        <Route path="/blotter" element={<BlotterPage />} />
        <Route path="/orders/new" element={<NewOrderPage />} />
        <Route path="/trades/:tradeId" element={<TradeDetailPage />} />
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;

